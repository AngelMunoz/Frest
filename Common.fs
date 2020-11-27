[<AutoOpen>]
module Frest.Common

open System
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt

open FSharp.Control.Tasks

open Microsoft.AspNetCore.Http
open Microsoft.IdentityModel.Tokens

open MongoDB.Bson

open Falco

open Domain


type ApiError = { Code: int; Message: seq<string> }

/// Internal URLs
[<RequireQualifiedAccess>]
module Urls =
    let ``/`` = "/"
    let ``/auth/login`` = "/auth/login"
    let ``/auth/signup`` = "/auth/signup"
    let ``/api/me`` = "/api/me"
    let ``/api/places`` = "/api/places"
    let ``/api/places/id`` = "/api/places/{:id}"

[<RequireQualifiedAccess>]
module Options =
    type ObjectIdConverter() =
        inherit JsonConverter<ObjectId>()

        override _.Read(reader: byref<Utf8JsonReader>, typeToConvert: Type, options: JsonSerializerOptions) =
            ObjectId.Parse(reader.GetString())

        override _.Write(writer: Utf8JsonWriter, value: ObjectId, options: JsonSerializerOptions) =
            writer.WriteStringValue(value.ToString())



    let JsonOptions =
        let opts = JsonSerializerOptions()
        opts.Converters.Add(JsonFSharpConverter())
        opts.Converters.Add(ObjectIdConverter())
        opts.AllowTrailingCommas <- true
        opts.ReadCommentHandling <- JsonCommentHandling.Skip
        opts

[<RequireQualifiedAccess>]
module Auth =

    let requiresAuthentication (successHandler: HttpHandler) (failedAuthHandler: string -> HttpHandler) =
        fun (ctx: HttpContext) ->
            task {
                if Security.Auth.isAuthenticated ctx
                then return! successHandler ctx
                else return! failedAuthHandler "Authentication Failed" ctx
            }


    let JwtSecret =
        Environment.GetEnvironmentVariable("FREST_JWT_TOKEN")
        |> Option.ofObj
        |> Option.defaultValue "much secret so wow :o"

    let generateJSONWebToken (claims: seq<Claim>) =

        let securityKey =
            SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret))

        let credentials =
            SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)

        let token =
            JwtSecurityToken(claims = claims, expires = DateTime.Now.AddDays(1.0), signingCredentials = credentials)

        JwtSecurityTokenHandler().WriteToken(token)
