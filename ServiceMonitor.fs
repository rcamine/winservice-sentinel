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

            match
                ServiceController.GetServices()
                |> Array.tryFind (fun s -> s.ServiceName = serviceName)
            with
            | None ->
                Log.Warning("Service '{ServiceName}' was not found on this computer.", serviceName)
                ServiceControllerStatus.Stopped // Return a default status
            | Some _ -> sc.Status
        with ex ->
            Log.Error(ex, "Error checking service status for service '{ServiceName}'", serviceName)
            raise ex

    let monitor
        (monitoringSettings: IOptionsMonitor<MonitoringSettings>)
        (emailSettings: IOptionsMonitor<EmailSettings>)
        =

        task {
            for KeyValue(serviceName, serviceOptions) in monitoringSettings.CurrentValue.Services do
                if serviceOptions.Enabled then
                    let serviceStatus = checkWinServiceStatus serviceName

                    if serviceStatus <> ServiceControllerStatus.Running then
                        let notifiers =
                            createNotifiers (serviceName, serviceOptions.Notifications, emailSettings.CurrentValue)

                        for notifier in notifiers do
                            use n = notifier
                            do! n.SendNotification()
                else
                    Log.Debug("Skipping service '{ServiceName}' as it is disabled in configuration", serviceName)
        }

    type ServiceMonitoringWorker
        (monitorSettings: IOptionsMonitor<MonitoringSettings>, emailSettings: IOptionsMonitor<EmailSettings>) =
        inherit BackgroundService()

        override _.ExecuteAsync(cancellationToken: CancellationToken) =
            task {
                while not cancellationToken.IsCancellationRequested do

                    let pollingIntervalMs =
                        match monitorSettings.CurrentValue.PollingIntervalMs with
                        | ms when ms <= 0 -> 10000 // Default if not configured or invalid
                        | ms -> ms

                    do! monitor monitorSettings emailSettings

                    do! Task.Delay(pollingIntervalMs, cancellationToken)
            }
            :> Task
