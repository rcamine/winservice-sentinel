namespace WinServiceSentinel

module ConfigModels =
    [<CLIMutable>]
    type NotificationOptions = { Type: string; Target: string }

    [<CLIMutable>]
    type ServiceOptions =
        { Name: string
          Notifications: NotificationOptions[] }

    [<CLIMutable>]
    type MonitoringSettings =
        { PollingIntervalMs: int
          Services: ServiceOptions[] }
