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

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<Program>>();
    var connectionString = builder.Configuration["RedisServer"];
    
    if (string.IsNullOrEmpty(connectionString))
    {
        logger.LogError("Redis connection string is missing");
        throw new InvalidOperationException("Redis connection string is required");
    }

    try
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 30000;
        options.SyncTimeout = 30000;
        options.ConnectRetry = 3;
        options.ReconnectRetryPolicy = new ExponentialRetry(5000);
        
        var connection = ConnectionMultiplexer.Connect(options);
        logger.LogInformation("Redis connection established successfully");
        return connection;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to connect to Redis: {Message}", ex.Message);
        throw;
    }
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
var secretKey = jwtSettings.GetValue<string>("SecretKey");
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is required");
}

var webSecretKey = Encoding.UTF8.GetBytes(secretKey);

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

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException($"Connection string for environment '{env}' is missing");
    }

    Console.WriteLine($"Using database connection for environment: {env}");

    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlServerOptions.CommandTimeout(60);
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
            .AllowCredentials(); // enable credential support
    });
});

// Cloudinary configuration
var cloudName = builder.Configuration["Cloudinary:CloudName"];
var apiKey = builder.Configuration["Cloudinary:ApiKey"];
var apiSecret = builder.Configuration["Cloudinary:ApiSecret"];

if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
{
    var cloudinaryAccount = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
    var cloudinary = new Cloudinary(cloudinaryAccount);
    builder.Services.AddSingleton(cloudinary);
}
else
{
    Console.WriteLine("Warning: Cloudinary configuration is incomplete. Image upload features may not work.");
}

var app = builder.Build();

await InitializeDatabaseAsync(app);

// Configure the HTTP request pipeline.
app.UseExceptionHandlingMiddleware();
app.UseRouting();
app.UseCors("corspolicy");

// Add health check endpoint
app.MapHealthChecks("/health");

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
    var retryDelay = TimeSpan.FromSeconds(3);

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempt {Attempt}/{MaxRetries}: Checking database connection...", i + 1, maxRetries);

            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("Cannot connect to database, retrying in {Delay} seconds...", retryDelay.TotalSeconds);
                await Task.Delay(retryDelay);
                continue;
            }

            logger.LogInformation("Database connection successful");

            var tablesCount = await context.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'");

            if (tablesCount == 0)
            {
                logger.LogInformation("Database appears to be empty, ensuring schema is created...");
                await context.Database.EnsureCreatedAsync();
            }

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully");
            }
            else
            {
                logger.LogInformation("No pending migrations found");
            }

            logger.LogInformation("Database initialization completed successfully");
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization attempt {Attempt} failed: {Message}", i + 1, ex.Message);

            if (i == maxRetries - 1)
            {
                logger.LogCritical("Database initialization failed after all retries. Application will start but may not work properly");
                return;
            }

            logger.LogInformation("Retrying in {Delay} seconds...", retryDelay.TotalSeconds);
            await Task.Delay(retryDelay);
        }
    }
}