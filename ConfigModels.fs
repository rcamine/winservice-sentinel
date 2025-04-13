namespace WinServiceSentinel

module ConfigModels =
    [<CLIMutable>]
    type ServiceListOptions = { Services: string[] }

    [<CLIMutable>]
    type MonitoringSettings = { PollingIntervalMs: int }
