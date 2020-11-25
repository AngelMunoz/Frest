module Frest.Provider

open System
open System.Threading.Tasks
open FSharp.Control.Tasks

open MongoDB.Bson
open MongoDB.Driver

open Mondocks.Queries
open Mondocks.Aggregation
open Mondocks.Types

open Frest.Domain

module internal Database =
    let private url =
        Environment.GetEnvironmentVariable("FREST_DB_URL")
        |> Option.ofObj
        |> Option.defaultValue "mongodb://localhost:27017"

    [<Literal>]
    let private DbName = "frestdb"

    [<Literal>]
    let UsersColName = "fre_users"

    let mongo () = MongoClient(url)

    let database () = mongo().GetDatabase(DbName)

[<RequireQualifiedAccess>]
module Places =
    0

[<RequireQualifiedAccess>]
module Users =
    open Database

    let TryFindByEmail (email: string): Task<Option<User>> =
        task {
            let findOne =
                find UsersColName {
                    filter {| email = email |}
                    limit 1
                }

            try
                let! result =
                    database()
                        .RunCommandAsync<FindResult<User>>(JsonCommand findOne)

                return result.cursor.firstBatch |> Seq.tryHead
            with ex ->
                eprintfn $"TryFindByEmail: [{ex.Message}]"
                return None
        }

    let TryFindByEmailWithPassword (email: string)
                                   : Task<Option<{| _id: ObjectId
                                                    email: string
                                                    password: string |}>> =
        task {
            let findOne =
                find UsersColName {
                    filter {| email = email |}
                    projection {| email = 1; password = 1 |}
                    limit 1
                }

            try
                let! result =
                    database()
                        .RunCommandAsync<FindResult<{| _id: ObjectId
                                                       email: string
                                                       password: string |}>>(JsonCommand findOne)

                return result.cursor.firstBatch |> Seq.tryHead
            with ex ->
                eprintfn $"TryFindByEmail: [{ex.Message}]"
                return None
        }

    let Exists (email: string): Task<bool> =
        task {
            let existsCmd =
                count {
                    collection UsersColName
                    query {| email = email |}
                }

            let! result =
                database()
                    .RunCommandAsync<CountResult>(JsonCommand existsCmd)

            return result.n > 0
        }

    let TryCreate (user: {| name: string
                            lastName: string
                            email: string
                            password: string |})
                  : Task<Option<User>> =
        task {
            let insertCmd =
                insert UsersColName { documents [ user ] }

            let! result =
                database()
                    .RunCommandAsync<InsertResult>(JsonCommand insertCmd)

            if result.n > 0 && result.ok = 1.0 then return! TryFindByEmail user.email else return None
        }
