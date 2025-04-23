using System.Data;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;

namespace TecChallenge.Application.Configurations;

public static class LoggingConfiguration
{
    public static void AddLoggingConfiguration(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var sinkOptions = new MSSqlServerSinkOptions { TableName = "Log" };

        var columnOptions = new ColumnOptions
        {
            Id =
            {
                DataType = SqlDbType.BigInt
            },
            TimeStamp =
            {
                DataType = SqlDbType.DateTime2
            }
        };
        columnOptions.Store.Remove(StandardColumn.Properties);
        columnOptions.AdditionalColumns =
        [
            new SqlColumn
            {
                ColumnName = "ApplicationName",
                DataType = SqlDbType.NVarChar,
                DataLength = 255
            }
        ];

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("HealthChecks", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", "tech-challenge-api")
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/log-.json",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 7,
                formatter: new JsonFormatter()
            )
            .WriteTo.MSSqlServer(
                connectionString,
                sinkOptions,
                columnOptions: columnOptions
            )
            .Filter.ByExcluding(logEvent => logEvent.RenderMessage().Contains("HTTP"))
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, true);
        });
    }
}