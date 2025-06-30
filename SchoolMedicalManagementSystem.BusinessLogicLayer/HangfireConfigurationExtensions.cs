using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer;

public static class HangfireConfigurationExtensions
{
    public static IServiceCollection AddCustomHangfire(this IServiceCollection services,
        IConfiguration configuration, string connectionString)
    {
        var hangfireConfig = configuration.GetSection("Hangfire");
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        services.AddHangfire(config =>
        {
            config.UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = true
            });

            var retryAttempts = hangfireConfig.GetValue<int>("RetryAttempts", 3);

            config.UseFilter(new AutomaticRetryAttribute { Attempts = retryAttempts });
        });

        var workerCount = hangfireConfig.GetValue<int>("WorkerCount",
            isDevelopment ? 2 : Environment.ProcessorCount * 2);

        var queues = hangfireConfig.GetSection("Queues").Get<string[]>()
                     ?? (isDevelopment ? new[] { "default" } : new[] { "critical", "high", "default", "low" });

        var schedulePollingInterval = hangfireConfig.GetValue<TimeSpan>("SchedulePollingInterval",
            TimeSpan.FromSeconds(isDevelopment ? 5 : 1));

        var heartbeatInterval = hangfireConfig.GetValue<TimeSpan>("HeartbeatInterval",
            TimeSpan.FromSeconds(30));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = workerCount;
            options.Queues = queues;
            options.ServerName = $"{Environment.MachineName}-medical-server-{Environment.ProcessId}";
            options.HeartbeatInterval = heartbeatInterval;
            options.SchedulePollingInterval = schedulePollingInterval;

            options.ServerTimeout = TimeSpan.FromMinutes(5);
            options.ShutdownTimeout = TimeSpan.FromMinutes(1);

            Console.WriteLine($"Hangfire Server Config:");
            Console.WriteLine($"  - Workers: {workerCount}");
            Console.WriteLine($"  - Queues: {string.Join(", ", queues)}");
            Console.WriteLine($"  - Polling: {schedulePollingInterval}");
            Console.WriteLine($"  - Heartbeat: {heartbeatInterval}");
        });

        return services;
    }
}