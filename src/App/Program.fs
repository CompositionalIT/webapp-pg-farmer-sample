open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open System
open Microsoft.Extensions.Configuration

let builder = WebApplication.CreateBuilder()
builder.Services.AddGiraffe() |> ignore
let webApp = builder.Build()

let helloWorld next (ctx: HttpContext) = task {
    let config = ctx.GetService<IConfiguration>()

    let dbDomainName =
        match config["DbDomainName"] with
        | null -> "NOT SET"
        | value -> value

    let description =
        match config["DbPassword"] with
        | null -> "NOT SET"
        | _ -> "SET"

    return! text $"""{DateTime.UtcNow}: DB Domain Name is {dbDomainName}. DB Password is {description}.""" next ctx
}

webApp.UseGiraffe helloWorld

[<EntryPoint>]
let main _ =
    webApp.Run()
    0