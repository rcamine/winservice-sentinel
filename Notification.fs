namespace WinServiceSentinel

open System
open System.Net
open System.Net.Mail
open System.Threading.Tasks
open Serilog
open ConfigModels

module Notification =
    type INotifier =
        abstract member ServiceName: string
        abstract member SendNotification: unit -> Task
        inherit IDisposable

    type EmailNotifier(serviceName: string, emails: string, emailSettings: EmailSettings) =
        member val Emails = emails
        member val ServiceName = serviceName
        member val Settings = emailSettings

        interface INotifier with
            member this.ServiceName = this.ServiceName

            member this.SendNotification() =
                try
                    use client = new SmtpClient(this.Settings.SmtpServer, this.Settings.SmtpPort)
                    client.Credentials <- NetworkCredential(this.Settings.Username, this.Settings.Password)
                    client.EnableSsl <- this.Settings.EnableSsl

                    client.Send(
                        this.Settings.FromAddress,
                        this.Emails,
                        $"Service Alert: {this.ServiceName} is down",
                        $"The service {this.ServiceName} is not responding and may require attention."
                    )

                    Log.Information(
                        "EmailNotifier: Sending email to {Email} for service {ServiceName}.",
                        this.Emails,
                        this.ServiceName
                    )
                with ex ->
                    Log.Error(
                        "EmailNotifier: Failed to send email to {Email} for service {ServiceName}. Error: {ErrorMessage}",
                        this.Emails,
                        this.ServiceName,
                        ex.Message
                    )

                Task.CompletedTask

            member this.Dispose() =
                // No resources to dispose for Email notifier
                ()

    type TeamsNotifier(serviceName: string, targetUrl: string) =
        let client = new Net.Http.HttpClient()

        member val TargetUrl = targetUrl
        member val ServiceName = serviceName

        interface INotifier with
            member this.ServiceName = this.ServiceName

            member this.SendNotification() =
                task {
                    let payload =
                        $"{{ \"text\": \"TeamsNotifier: Service {this.ServiceName} is down.\" }}"

                    use content =
                        new Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json")

                    let! response = client.PostAsync(this.TargetUrl, content) |> Async.AwaitTask

                    if response.IsSuccessStatusCode then
                        Log.Information(
                            "TeamsNotifier: Notification sent successfully for service {ServiceName}.",
                            this.ServiceName
                        )
                    else
                        Log.Error(
                            "TeamsNotifier: Failed to send notification for service {ServiceName}. Status Code: {StatusCode}",
                            this.ServiceName,
                            response.StatusCode
                        )
                }

            member this.Dispose() = client.Dispose()

    type SlackNotifier(serviceName: string, targetUrl: string) =
        let client = new Net.Http.HttpClient()

        member val TargetUrl = targetUrl
        member val ServiceName = serviceName

        interface INotifier with
            member this.ServiceName = this.ServiceName

            member this.SendNotification() =
                task {
                    let payload =
                        $"{{ \"text\": \"SlackNotifier: Service {this.ServiceName} is down.\" }}"

                    use content =
                        new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json")

                    let! response = client.PostAsync(this.TargetUrl, content) |> Async.AwaitTask

                    if response.IsSuccessStatusCode then
                        Log.Information(
                            "SlackNotifier: Notification sent successfully for service {ServiceName}.",
                            this.ServiceName
                        )
                    else
                        Log.Error(
                            "SlackNotifier: Failed to send notification for service {ServiceName}. Status Code: {StatusCode}",
                            this.ServiceName,
                            response.StatusCode
                        )
                }

            member this.Dispose() = client.Dispose()

    let createNotifiers
        (serviceName: string, notificationSettings: NotificationOptions array, emailSettings: EmailSettings)
        =
        notificationSettings
        |> Array.choose (fun notification ->
            match notification.Type with
            | "Email" -> Some(new EmailNotifier(serviceName, notification.Target, emailSettings) :> INotifier)
            | "Teams" -> Some(new TeamsNotifier(serviceName, notification.Target) :> INotifier)
            | "Slack" -> Some(new SlackNotifier(serviceName, notification.Target) :> INotifier)
            | _ -> None)
