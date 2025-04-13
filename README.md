# WinServiceSentinel

WinServiceSentinel is a Windows service monitoring tool built using F#. It periodically checks the status of specified Windows services and sends notifications if any service is not running. The tool is designed to be highly configurable and supports dynamic updates to its configuration without requiring a restart.

## Features

- Monitors the status of multiple Windows services.
- Sends notifications via email, Microsoft Teams, and Slack.
- Configurable polling interval and service list via `appsettings.json`.
- Supports dynamic configuration updates using `IOptionsMonitor`.
- Logs events using Serilog.
- Runs as a Windows Service.

## Prerequisites

- .NET 9.0 SDK or later
- Windows operating system

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/your-repo/winservice-sentinel.git
   ```
2. Navigate to the project directory:
   ```bash
   cd winservice-sentinel
   ```
3. Build the project:
   ```bash
   dotnet build
   ```
4. Publish the project as a Windows service:
   ```bash
   dotnet publish -c Release -o ./publish
   ```
5. Install the service using `sc.exe` or a similar tool:
   ```bash
   sc create WinServiceSentinel binPath= "<path-to-publish-folder>\WinServiceSentinel.exe"
   ```

## Configuration

The application is configured using `appsettings.json`. Below is an example configuration:

```json
{
  "MonitoringSettings": {
    "PollingIntervalMs": 5000,
    "Services": {
      "ServiceName1": {
        "Notifications": [
          { "Type": "Email", "Target": "admin@example.com;admin2@example.com" },
          { "Type": "Teams", "Target": "https://example.com/teams-webhook" },
          { "Type": "Slack", "Target": "https://example.com/slack-webhook" }
        ]
      },
      "ServiceName2": {
        "Notifications": [
          { "Type": "Email", "Target": "admin@example.com" },
          { "Type": "Teams", "Target": "https://example.com/teams-webhook" }
        ]
      }
    }
  }
}
```

### Key Settings

- `MonitoringSettings.PollingIntervalMs`: The polling interval in milliseconds.
- `MonitoringSettings.Services`: An object with service names as keys.
  - Each key is the name of a Windows service to monitor.
  - Each service has a `Notifications` array containing notification configurations:
    - `Type`: The notification type (`Email`, `Teams`, or `Slack`).
    - `Target`: The notification destination (semicolon-separated email addresses or webhook URLs).

## Logging

The application uses Serilog for logging. Logs are written to the console by default. You can customize the logging configuration in `Program.fs`.

## Development

### Project Structure

- `Program.fs`: Entry point of the application.
- `ServiceMonitor.fs`: Contains the logic for monitoring services.
- `Notification.fs`: Defines the notification system.
- `ConfigModels.fs`: Contains configuration models for strongly-typed settings.
- `appsettings.json`: Configuration file for the application.

### Running Locally

To run the application locally:
```bash
dotnet run
```

### Testing Configuration Changes

To test dynamic configuration updates:
1. Modify `appsettings.json` while the application is running.
2. The changes will be automatically detected and applied.

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

## Support

For issues or questions, please open an issue on the GitHub repository.
