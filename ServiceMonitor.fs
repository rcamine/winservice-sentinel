namespace WinServiceSentinel

open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Options
open System.ServiceProcess
open Notification
open Serilog
open ConfigModels

module ServiceMonitor =

    let checkWinServiceStatus serviceName =
        try
            use sc = new ServiceController(serviceName)

            if
                not (
                    ServiceController.GetServices()
                    |> Array.exists (fun s -> s.ServiceName = serviceName)
                )
            then
                Log.Warning("Service '{ServiceName}' was not found on this computer.", serviceName)
                ServiceControllerStatus.Stopped // Return a default status
            else
                sc.Status
        with ex ->
            Log.Error(ex, "Error checking service status for service '{ServiceName}'", serviceName)
            raise ex

    type ServiceMonitoringWorker
        (
            serviceOptions: IOptionsMonitor<ServiceListOptions>,
            monitoringOptions: IOptionsMonitor<MonitoringSettings>,
            notifiers: INotifier list
        ) =
        inherit BackgroundService()

        override _.ExecuteAsync(cancellationToken: CancellationToken) =
            task {
                while not cancellationToken.IsCancellationRequested do

                    let serviceNames = serviceOptions.CurrentValue.Services |> Array.toList

                    let pollingIntervalMs =
                        match monitoringOptions.CurrentValue.PollingIntervalMs with
                        | ms when ms <= 0 -> 10000 // Default if not configured or invalid
                        | ms -> ms

                    for serviceName in serviceNames do
                        let status = checkWinServiceStatus serviceName

                        if status <> ServiceControllerStatus.Running then
                            for notifier in notifiers do
                                do! notifier.SendNotification serviceName

                    do! Task.Delay(pollingIntervalMs, cancellationToken)
            }
            :> Task
