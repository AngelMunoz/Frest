module Frest.Program

open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Authentication.JwtBearer
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.IdentityModel.Tokens
open System.Text
open System

// ------------
// Web app
// ------------
let endpoints =
    [ post Urls.``/auth/login`` Value.Controller.login
      post Urls.``/auth/signup`` Value.Controller.signup
      get Urls.``/api/me`` Value.Controller.me
      get Urls.``/api/places`` Value.Controller.getPlaces
      post Urls.``/api/places`` Value.Controller.addPlaces
      put Urls.``/api/places/id`` Value.Controller.updatePlaces
      delete Urls.``/api/places/id`` Value.Controller.deletePlaces ]

// ------------
// Register services
// ------------
let configureServices (services: IServiceCollection) =
    services.AddAntiforgery().AddFalco() |> ignore

    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme,
                      (fun options ->
                          let tokenParams = TokenValidationParameters()
                          tokenParams.ValidateIssuer <- false
                          tokenParams.ValidateAudience <- false
                          tokenParams.ValidateIssuerSigningKey <- true
                          tokenParams.IssuerSigningKey <- SymmetricSecurityKey(Encoding.UTF8.GetBytes(Auth.JwtSecret))
                          options.TokenValidationParameters <- tokenParams))
    |> ignore


// ------------
// Activate middleware
// ------------
let configureApp (app: IApplicationBuilder) =
    app.UseAuthentication() |> ignore
    app.UseStaticFiles().UseFalco(endpoints) |> ignore

[<EntryPoint>]
let main args =
    try
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webhost ->
                webhost
                    .ConfigureServices(configureServices)
                    .Configure(configureApp)
                |> ignore)
            .Build()
            .Run()

        0
    with ex ->
        printfn "%s\n\n%s" ex.Message ex.StackTrace
        -1
