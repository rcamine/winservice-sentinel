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

    let monitor serviceNames (monitoringOptions: IOptionsMonitor<MonitoringSettings>) =

        task {
            for serviceName in serviceNames do
                let status = checkWinServiceStatus serviceName

                let notificationSettings =
                    monitoringOptions.CurrentValue.Services
                    |> Array.tryFind (fun service -> service.Name = serviceName)
                    |> function
                        | Some service -> service.Notifications
                        | None -> [||] // Return a default value if not found

                if status <> ServiceControllerStatus.Running then
                    let notifiers = createNotifiers (serviceName, notificationSettings)

                    for notifier in notifiers do
                        use n = notifier
                        do! n.SendNotification()
        }

    type ServiceMonitoringWorker(monitoringOptions: IOptionsMonitor<MonitoringSettings>) =
        inherit BackgroundService()

        override _.ExecuteAsync(cancellationToken: CancellationToken) =
            task {
                while not cancellationToken.IsCancellationRequested do

                    let serviceNames = monitoringOptions.CurrentValue.Services |> Seq.map _.Name

                    let pollingIntervalMs =
                        match monitoringOptions.CurrentValue.PollingIntervalMs with
                        | ms when ms <= 0 -> 10000 // Default if not configured or invalid
                        | ms -> ms

                    do! monitor serviceNames monitoringOptions

                    do! Task.Delay(pollingIntervalMs, cancellationToken)
            }
            :> Task
