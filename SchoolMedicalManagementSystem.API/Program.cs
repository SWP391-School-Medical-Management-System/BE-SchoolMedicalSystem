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

// Force Production environment if not explicitly set
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    builder.Environment.EnvironmentName = "Production";
}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

Console.WriteLine($"Running in {builder.Environment.EnvironmentName} environment");

// Add health checks
builder.Services.AddHealthChecks();

// Redis configuration
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

// Add services with better error handling for Background Service
try
{
    builder.Services.AddDataAccessLayer(builder.Configuration);
    builder.Services.AddBusinessLogicLayer(builder.Configuration);
}
catch (Exception ex)
{
    Console.WriteLine($"Error adding services: {ex.Message}");
    throw;
}

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
            NameClaimType = "uid",
            ClockSkew = TimeSpan.Zero
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

// Database configuration with proper connection string selection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var env = builder.Environment.EnvironmentName;

    // Always use production connection string for containers and production
    var connectionString = builder.Configuration.GetConnectionString("production");

    // Only use local connection if explicitly in Development and local exists
    if (env.Equals("Development", StringComparison.OrdinalIgnoreCase))
    {
        var localConnectionString = builder.Configuration.GetConnectionString("local");
        if (!string.IsNullOrEmpty(localConnectionString))
        {
            connectionString = localConnectionString;
        }
    }

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException($"No connection string found for environment '{env}'");
    }

    Console.WriteLine($"Using database connection for environment: {env}");

    // Replace localhost with sqlserver for Docker environments
    if (connectionString.Contains("localhost") &&
        (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
         !env.Equals("Development", StringComparison.OrdinalIgnoreCase)))
    {
        connectionString = connectionString.Replace("localhost", "sqlserver");
        Console.WriteLine("Replaced localhost with sqlserver for container environment");
    }

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
                "http://localhost:5173", // frontend dev
                "https://school-medical-system.vercel.app", // prod vercel
                "http://schoolmedicalsystem.ddns.net", // No-IP domain HTTP
                "https://schoolmedicalsystem.ddns.net" // No-IP domain HTTPS default port
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure background service exception behavior
builder.Services.Configure<HostOptions>(hostOptions =>
{
    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
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

// Simple database initialization
await QuickDatabaseCheckAsync(app);

// Configure the HTTP request pipeline
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

Console.WriteLine("School Medical System API is starting...");

app.Run();

async Task QuickDatabaseCheckAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Quick database connection test...");

        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("Database connection successful!");

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations completed!");
            }
            else
            {
                logger.LogInformation("Database is up to date!");
            }
        }
        else
        {
            logger.LogWarning("Cannot connect to database initially, but application will continue...");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization error: {Message}", ex.Message);
        logger.LogInformation("Application will continue without database...");
    }

    logger.LogInformation("Database check completed - continuing with application startup!");
}
