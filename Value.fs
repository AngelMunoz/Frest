module Frest.Value

open System.Security.Claims
open System.Threading.Tasks

open FSharp.Control.Tasks

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

open Falco


open Frest.Domain

module Model =
    open Frest.Provider

    type ManagedError =
        | AlreadyExists
        | EmptyValues
        | FailedToCreate
        | DatabaseError

    [<RequireQualifiedAccess>]
    module User =
        open type BCrypt.Net.BCrypt

        let login (user: {| email: string; password: string |}) =
            task {
                match! Users.TryFindByEmailWithPassword user.email with
                | Some found -> return EnhancedVerify(user.password, found.password)
                | None -> return false
            }

        let signup (user: {| name: string
                             lastName: string
                             email: string
                             password: string |})
                   : Task<Result<User, ManagedError>> =
            task {
                let! exists = Users.Exists user.email

                if exists then
                    return Error AlreadyExists
                else

                    let! user =
                        Users.TryCreate
                            {| user with
                                   password = EnhancedHashPassword user.password |}

                    match user with
                    | None -> return Error FailedToCreate
                    | Some user -> return Ok user

            }

    [<RequireQualifiedAccess>]
    module Place =

        let getPlaces (owner: string) (pagination: PaginationParams) (logger: ILogger) =
            task {
                let! result = Places.FindMyPlaces owner pagination

                return
                    match result with
                    | Ok result -> Ok result
                    | Error ex ->
                        logger.LogWarning(ex, $"Failed to get places for {owner}")
                        Error DatabaseError
            }


        let addPlaces (owner: string)
                      (places: seq<{| name: string
                                      lat: float
                                      lon: float |}>)
                      (logger: ILogger)
                      =
            task {
                let! result =
                    Places.TryAddPlaces
                        (places
                         |> Seq.map (fun place -> {| place with owner = owner |}))

                return
                    match result with
                    | Ok result -> Ok result
                    | Error err ->
                        logger.LogWarning(err, $"Failed to Save places for {owner}")
                        Error DatabaseError

            }

        let updatePlace (places: seq<Place>) (logger: ILogger) =
            task {
                match! Places.TryUpdatePlaces places with
                | Ok bool -> return Ok bool
                | Error ex ->
                    logger.LogWarning(ex, $"Failed to update Place %A{places |> Seq.map (fun p -> p._id.ToString())}")
                    return Error DatabaseError
            }

        let deletePlaces (places: seq<Place>) (logger: ILogger) =
            task {
                match! Places.TryDeletePlaces places with
                | Ok deleted -> return Ok deleted
                | Error ex ->
                    logger.LogWarning(ex, $"Failed to delete places %A{places |> Seq.map (fun p -> p._id.ToString())}")
                    return Error DatabaseError
            }


module Controller =
    open Model

    let private getSignInClaims (email: string) =
        [ Claim(ClaimTypes.Name, email)
          Claim(ClaimTypes.Email, email) ]

    let private bindJsonError (error: string) =
        Response.withStatusCode 400
        >> Response.ofPlainText (sprintf "Invalid JSON: %s" error)

    let private failedAuthError (error: string) =
        Response.withStatusCode 401
        >> Response.ofJson {| message = $"Request failed to authenticate: [%s{error}]" |}

    let private loginHandler (payload: {| email: string; password: string |}) (ctx: HttpContext) =
        task {
            let! loggedIn = User.login payload

            if not loggedIn then
                return!
                    (Response.withStatusCode 401
                     >> Response.ofJson {| message = "Wrong Credentials" |})
                        ctx

            let claims = getSignInClaims payload.email
            let token = Auth.generateJSONWebToken claims

            return! Response.ofJson {| token = token |} ctx
        }

    let signUpHandler (payload: {| name: string
                                   lastName: string
                                   email: string
                                   password: string |})
                      (ctx: HttpContext)
                      =
        task {
            let! signedUp = User.signup payload

            match signedUp with
            | Ok user ->
                let claims = getSignInClaims payload.email
                let token = Auth.generateJSONWebToken claims

                return! Response.ofJsonOptions Options.JsonOptions {| token = token; user = user |} ctx
            | Error apierror ->
                return!
                    match apierror with
                    | AlreadyExists ->
                        (Response.withStatusCode 400
                         >> Response.ofJson {| message = "Email is in use." |})
                            ctx
                    | FailedToCreate ->
                        (Response.withStatusCode 500
                         >> Response.ofJson {| message = "Failed to create user." |})
                            ctx
                    | _ ->
                        (Response.withStatusCode 500
                         >> Response.ofJson {| message = "An Unknown Error Ocurred" |})
                            ctx
        }

    let private getPlacesHandler: HttpHandler =
        fun (ctx: HttpContext) ->
            task {
                let logger = ctx.GetLogger("Places")

                let user =
                    ctx.GetUser()
                    |> Option.map
                        (fun user ->
                            user.FindFirstValue(ClaimTypes.Name)
                            |> Option.ofObj)
                    |> Option.flatten

                let qr = ctx.Request.GetQueryReader()

                let pagination =
                    { page = defaultArg (qr.TryGetInt "page") 1
                      limit = defaultArg (qr.TryGetInt "limit") 10 }

                match user with
                | None ->

                    return!
                        (Response.withStatusCode 422
                         >> Response.ofJson {| message = "Unable to complete the request" |})
                            ctx
                | Some owner ->
                    let! places = Provider.Places.FindMyPlaces owner pagination

                    match places with
                    | Error _ ->
                        return!
                            (Response.withStatusCode 422
                             >> Response.ofJson {| message = "Unable to complete the request" |})
                                ctx
                    | Ok places -> return! Response.ofJsonOptions Options.JsonOptions places ctx
            }

    let private addPlacesHandler (places: seq<{| name: string
                                                 lat: float
                                                 lon: float |}>)
                                 (ctx: HttpContext)
                                 =
        task {
            let logger = ctx.GetLogger("Places")

            let user =
                ctx.GetUser()
                |> Option.map
                    (fun user ->
                        user.FindFirstValue(ClaimTypes.Name)
                        |> Option.ofObj)
                |> Option.flatten

            match user with
            | None ->
                return!
                    (Response.withStatusCode 422
                     >> Response.ofJson {| message = "Unable to complete the request" |})
                        ctx
            | Some email ->
                let! saved = Place.addPlaces email places logger

                match saved with
                | Ok saved when saved ->
                    return!
                        (Response.withStatusCode 201
                         >> Response.ofJson {| saved = saved |})
                            ctx
                | Ok saved when not saved ->
                    return!
                        (Response.withStatusCode 400
                         >> Response.ofJson {| saved = saved |})
                            ctx
                | _ ->
                    return!
                        (Response.withStatusCode 422
                         >> Response.ofJson {| message = "Unable to complete the request" |})
                            ctx
        }

    let private updatePlacesHandler (ctx: HttpContext) =
        task {
            let! place = Request.tryBindJsonOptions<Place> Options.JsonOptions ctx

            match place with
            | Error err -> return! bindJsonError err ctx
            | Ok place ->
                let logger = ctx.GetLogger("Places")
                let! result = Place.updatePlace [ place ] logger

                match result with
                | Ok updated -> return! Response.ofJson {| updated = updated |} ctx
                | Error _ ->
                    return!
                        (Response.withStatusCode 500
                         >> Response.ofJson {| updated = false |})
                            ctx
        }

    let private deletePlacesHandler (ctx: HttpContext) =
        task {
            let! place = Request.tryBindJsonOptions<seq<Place>> Options.JsonOptions ctx

            match place with
            | Error err -> return! bindJsonError err ctx
            | Ok places ->
                let logger = ctx.GetLogger("Places")
                let! result = Place.deletePlaces places logger

                match result with
                | Ok deleted -> return! Response.ofJson {| deleted = deleted |} ctx
                | Error _ ->
                    return!
                        (Response.withStatusCode 500
                         >> Response.ofJson {| updated = false |})
                            ctx

                return! Response.ofJson {| ok = true |} ctx
        }

    /// HTTP POST /value/create
    let login: HttpHandler =
        Request.bindJson loginHandler bindJsonError


    let signup: HttpHandler =
        Request.bindJson signUpHandler bindJsonError


    let me: HttpHandler =
        Auth.requiresAuthentication
            (fun (ctx: HttpContext) ->
                task {
                    match ctx.GetUser() with
                    | None -> return! (Response.withStatusCode 404 >> Response.ofEmpty) ctx
                    | Some user ->
                        let email = user.FindFirstValue(ClaimTypes.Name)

                        match! Provider.Users.TryFindByEmail email with
                        | None -> return! (Response.withStatusCode 404 >> Response.ofEmpty) ctx
                        | Some user -> return! Response.ofJsonOptions Options.JsonOptions user ctx
                })
            failedAuthError

    let getPlaces: HttpHandler =
        Auth.requiresAuthentication getPlacesHandler failedAuthError

    let addPlaces: HttpHandler =
        Auth.requiresAuthentication (Request.bindJson addPlacesHandler bindJsonError) failedAuthError

    let updatePlaces: HttpHandler =
        Auth.requiresAuthentication updatePlacesHandler failedAuthError

    let deletePlaces: HttpHandler =
        Auth.requiresAuthentication deletePlacesHandler failedAuthError
