namespace WinServiceSentinel

open System
open System.Threading.Tasks
open Serilog
open ConfigModels

module Notification =
    type INotifier =
        abstract member ServiceName: string
        abstract member SendNotification: unit -> Task
        inherit IDisposable

    type EmailNotifier(serviceName: string, emails: string array) =
        member val Emails = emails
        member val ServiceName = serviceName

        interface INotifier with
            member this.ServiceName = this.ServiceName

            member this.SendNotification() =
                this.Emails
                |> Array.iter (fun email ->
                    Log.Information(
                        "EmailNotifier: Sending email to {Email} for service {ServiceName}.",
                        email,
                        this.ServiceName
                    ))

                Task.CompletedTask

            member this.Dispose() =
                // No resources to dispose for Email notifier
                ()

    type TeamsNotifier(serviceName: string, targetUrl: string) =
        let client = new System.Net.Http.HttpClient()

        member val TargetUrl = targetUrl
        member val ServiceName = serviceName

        interface INotifier with
            member this.ServiceName = this.ServiceName

            member this.SendNotification() =
                task {
                    let payload =
                        $"{{ \"text\": \"TeamsNotifier: Service {this.ServiceName} is down.\" }}"

                    use content =
                        new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json")

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
        let client = new System.Net.Http.HttpClient()

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

    let createNotifiers (serviceName: string, notificationSettings: NotificationOptions array) =
        notificationSettings
        |> Array.choose (fun notification ->
            match notification.Type with
            | "Email" -> Some(new EmailNotifier(serviceName, notification.Target.Split(";")) :> INotifier)
            | "Teams" -> Some(new TeamsNotifier(serviceName, notification.Target) :> INotifier)
            | "Slack" -> Some(new SlackNotifier(serviceName, notification.Target) :> INotifier)
            | _ -> None)
