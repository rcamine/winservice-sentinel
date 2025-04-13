namespace WinServiceSentinel

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open ServiceMonitor
open Serilog
open ConfigModels

module Program =

    [<EntryPoint>]
    let main argv =
        Log.Logger <- LoggerConfiguration().WriteTo.Console().CreateLogger()

        let builder =
            Host
                .CreateDefaultBuilder(argv)
                .UseSerilog()
                .UseWindowsService()
                .ConfigureAppConfiguration(fun _ config ->
                    config.AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
                    |> ignore)
                .ConfigureServices(fun hostContext services ->

                    services.Configure<MonitoringSettings>(hostContext.Configuration.GetSection "MonitoringSettings")
                    |> ignore

                    services.AddHostedService<ServiceMonitoringWorker>() |> ignore)

        try
            builder.Build().Run()
            0
        with ex ->
            Log.Fatal(ex, "Host terminated unexpectedly")
            1
