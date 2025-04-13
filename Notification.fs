namespace WinServiceSentinel

open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Serilog

module Notification =
    type INotifier =
        abstract member SendNotification: string -> Task

    type EmailNotifier() =
        interface INotifier with
            member _.SendNotification message =
                Log.Information("EmailNotifier: Service {Message} is down.", message)
                Task.CompletedTask

    type TeamsNotifier() =
        interface INotifier with
            member _.SendNotification message =
                Log.Information("TeamsNotifier: Service {Message} is down.", message)
                Task.CompletedTask

    type SlackNotifier() =
        interface INotifier with
            member _.SendNotification message =
                Log.Information("SlackNotifier: Service {Message} is down.", message)
                Task.CompletedTask

    let createNotifiers (configuration: IConfiguration) =
        [ EmailNotifier() :> INotifier
          TeamsNotifier() :> INotifier
          SlackNotifier() :> INotifier ]
