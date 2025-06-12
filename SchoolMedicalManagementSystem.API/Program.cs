using System.Text;
using System.Text.Json.Serialization;
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
                "http://localhost:3000" // frontend dev
                // "https://fe-smartaidoor.vercel.app"    // prod
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // bật credential support
    });
});

try
{
    var redis = ConnectionMultiplexer.Connect(builder.Configuration["RedisServer"]);
    Console.WriteLine("Redis connected successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to Redis: {ex.Message}");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandlingMiddleware();
app.UseRouting();
app.UseCors("corspolicy");

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();