using DurableFunctionsDI;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


// See https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows#start-up-and-configuration
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(s =>
    {
        s.AddApplicationInsightsTelemetryWorkerService();
        s.ConfigureFunctionsApplicationInsights();
        //s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
        //s.Configure<LoggerFilterOptions>(options =>
        //{
        //    // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
        //    // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
        //    LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
        //        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

        //    if (toRemove is not null)
        //    {
        //        options.Rules.Remove(toRemove);
        //    }
        //});

        // See https://github.com/Azure/azure-functions-dotnet-worker/issues/2184
        s.Configure<KestrelServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
        });

        s.AddSingleton<IFunction, Function1>();
    })
    .Build();

host.Run();
