module Frest.Value

open System.Security.Claims
open System.Threading.Tasks

open FSharp.Control.Tasks

open Microsoft.AspNetCore.Http

open MongoDB.Bson

open Falco

open type BCrypt.Net.BCrypt

open Frest.Domain
open Frest.Provider

module Model =
    [<RequireQualifiedAccess>]
    module User =
        // Errors
        type ManagedError =
            | AlreadyExists
            | EmptyValues
            | FailedToCreate

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


module Controller =
    open Model

    let private getSignInClaims (email: string) =
        [ Claim(ClaimTypes.Name, email)
          Claim(ClaimTypes.Email, email) ]

    let private bindJsonError (error: string) =
        Response.withStatusCode 400
        >> Response.ofPlainText (sprintf "Invalid JSON: %s" error)

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

                return! Response.ofJsonOptions Options.jsonOptions {| token = token; user = user |} ctx
            | Error apierror ->
                return!
                    match apierror with
                    | User.ManagedError.AlreadyExists ->
                        (Response.withStatusCode 400
                         >> Response.ofJson {| message = "Email is in use." |})
                            ctx
                    | User.ManagedError.FailedToCreate ->
                        (Response.withStatusCode 500
                         >> Response.ofJson {| message = "Failed to create user." |})
                            ctx
                    | _ ->
                        (Response.withStatusCode 500
                         >> Response.ofJson {| message = "An Unknown Error Ocurred" |})
                            ctx
        }

    /// HTTP POST /value/create
    let login: HttpHandler =
        Request.bindJson loginHandler bindJsonError


    let signup: HttpHandler =
        Request.bindJson signUpHandler bindJsonError
