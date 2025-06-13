using System.Text;
using System.Text.Json.Serialization;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SchoolMedicalManagementSystem.API.Middlewares;
using SchoolMedicalManagementSystem.API.OperationFilters;
using SchoolMedicalManagementSystem.BusinessLogicLayer;
using SchoolMedicalManagementSystem.DataAccessLayer;
using SchoolMedicalManagementSystem.DataAccessLayer.Context;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    return ConnectionMultiplexer.Connect(builder.Configuration["RedisServer"]);
});

builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Services.AddBusinessLogicLayer(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

var jwtSettings = builder.Configuration.GetSection("JWT");
var webSecretKey = Encoding.UTF8.GetBytes(jwtSettings.GetValue<string>("SecretKey"));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuers = new[] { jwtSettings.GetValue<string>("ValidIssuer") },
            ValidAudiences = new[] { jwtSettings.GetValue<string>("ValidAudience") },
            IssuerSigningKey = new SymmetricSecurityKey(webSecretKey),
            RoleClaimType = "r",
            NameClaimType = "uid"
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Description = "Please enter your token with this format: 'Bearer YOUR_TOKEN'",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Name = "Bearer", In = ParameterLocation.Header,
                Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
            },
            new List<string>()
        }
    });
    options.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString("00:00:00") });
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "School Medical System API", Version = "v1" });
    options.OperationFilter<GenericResponseTypeOperationFilter>();
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var env = builder.Environment.EnvironmentName;
    var connectionString = env.Equals("Production", StringComparison.OrdinalIgnoreCase)
        ? builder.Configuration.GetConnectionString("production")
        : builder.Configuration.GetConnectionString("local");

    Console.WriteLine($"Using database connection for environment: {env}, ConnectionString: {connectionString}");

    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("corspolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000", // frontend dev
                "https://school-medical-system.vercel.app" // prod
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // bật credential support
    });
});

// Test Redis connection
try
{
    var redis = ConnectionMultiplexer.Connect(builder.Configuration["RedisServer"]);
    Console.WriteLine("Redis connected successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to Redis: {ex.Message}");
}

// Cloudinary
var cloudName = builder.Configuration["Cloudinary:CloudName"];
var apiKey = builder.Configuration["Cloudinary:ApiKey"];
var apiSecret = builder.Configuration["Cloudinary:ApiSecret"];
var cloudinaryAccount = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
var cloudinary = new Cloudinary(cloudinaryAccount);
builder.Services.AddSingleton(cloudinary);

var app = builder.Build();

await InitializeDatabaseAsync(app);

// Configure the HTTP request pipeline.
app.UseExceptionHandlingMiddleware();
app.UseRouting();
app.UseCors("corspolicy");

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var maxRetries = 30;
    var retryDelay = TimeSpan.FromSeconds(2);

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation($"Attempt {i + 1}/{maxRetries}: Checking database connection...");

            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("Cannot connect to database, retrying...");
                await Task.Delay(retryDelay);
                continue;
            }

            logger.LogInformation("Database connection successful.");

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("No pending migrations found.");
            }

            var tablesExist = await context.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'") > 0;

            if (tablesExist)
            {
                logger.LogInformation("Database schema verified successfully.");
            }
            else
            {
                logger.LogWarning("Database tables not found. Creating database schema...");
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("Database schema created successfully.");
            }

            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Database initialization attempt {i + 1} failed: {ex.Message}");

            if (i == maxRetries - 1)
            {
                logger.LogCritical(
                    "Database initialization failed after all retries. Application will continue but may not work properly.");
                return;
            }

            logger.LogInformation($"Retrying in {retryDelay.TotalSeconds} seconds...");
            await Task.Delay(retryDelay);
        }
    }
}