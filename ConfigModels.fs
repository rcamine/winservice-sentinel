namespace WinServiceSentinel

open System.Collections.Generic

module ConfigModels =
    [<CLIMutable>]
    type NotificationOptions = { Type: string; Target: string }

    [<CLIMutable>]
    type ServiceOptions =
        { Enabled: bool
          Notifications: NotificationOptions[] }

    [<CLIMutable>]
    type EmailSettings =
        { SmtpServer: string
          SmtpPort: int
          Username: string
          Password: string
          EnableSsl: bool
          FromAddress: string }

    [<CLIMutable>]
    type MonitoringSettings =
        { PollingIntervalMs: int
          Services: Dictionary<string, ServiceOptions> }
