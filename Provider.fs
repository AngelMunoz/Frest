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

    [<Literal>]
    let PlacesColName = "fre_places"

    let mongo = lazy (MongoClient(url))

    let database = lazy (mongo.Value.GetDatabase(DbName))

[<RequireQualifiedAccess>]
module Places =
    open Database

    let TryUpdatePlaces (places: seq<Place>): Task<Result<bool, exn>> =
        task {
            let records =
                places
                |> Seq.map
                    (fun place ->
                        { q =
                              {| _id = {| ``$oid`` = place._id |}
                                 owner = place.owner |}
                          u =
                              {| place with
                                     _id = {| ``$oid`` = place._id |} |}
                          upsert = Some false
                          multi = Some false
                          collation = None
                          arrayFilters = None
                          hint = None })

            try
                let! result =
                    database.Value.RunCommandAsync<UpdateResult>(JsonCommand(update PlacesColName { updates records }))

                return
                    Ok
                        (result.nModified >= 0
                         && result.n >= 0
                         && result.ok = 1.0)
            with ex ->
                eprintfn $"TryUpdatePlaces: [{ex.Message}]"
                return Error ex
        }

    let TryAddPlaces (places: seq<{| name: string
                                     owner: string
                                     lat: float
                                     lon: float |}>)
                     : Task<Result<bool, exn>> =
        task {
            try
                let! result =
                    database.Value.RunCommandAsync<InsertResult>(JsonCommand(insert PlacesColName { documents places }))

                return Ok(result.n > 0 && result.ok = 1.0)
            with ex ->
                eprintfn $"TryAddPlaces: [{ex.Message}]"
                return Error ex
        }

    let TryDeletePlaces (places: seq<Place>) =
        task {
            let records =
                places
                |> Seq.map
                    (fun place ->
                        { q =
                              {| place with
                                     _id = {| ``$oid`` = place._id |} |}
                          limit = 1
                          collation = None
                          hint = None
                          comment = None })

            try
                let! result =
                    database.Value.RunCommandAsync<DeleteResult>(JsonCommand(delete PlacesColName { deletes records }))

                return Ok(result.n > 0 && result.ok = 1.0)
            with ex ->
                eprintfn $"TryDeletePlaces: [{ex.Message}]"
                return Error ex

        }

    let FindMyPlaces (owner: string) (pagination: PaginationParams): Task<Result<PaginatedResult<Place>, exn>> =
        task {
            let queryFilter = {| owner = owner |}

            let findCmd =
                find PlacesColName {
                    filter queryFilter
                    limit pagination.limit
                    skip ((pagination.page - 1) * pagination.limit)
                }

            let countCmd =
                count {
                    collection PlacesColName
                    query queryFilter
                }

            try
                let! result = database.Value.RunCommandAsync<FindResult<Place>>(JsonCommand findCmd)

                let! count = database.Value.RunCommandAsync<CountResult>(JsonCommand countCmd)

                return
                    Ok
                        { count = count.n
                          items = result.cursor.firstBatch }
            with ex -> return Error ex
        }

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
                let! result = database.Value.RunCommandAsync<FindResult<User>>(JsonCommand findOne)

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
                    database.Value.RunCommandAsync<FindResult<{| _id: ObjectId
                                                                 email: string
                                                                 password: string |}>>
                        (JsonCommand findOne)

                return result.cursor.firstBatch |> Seq.tryHead
            with ex ->
                eprintfn $"TryFindByEmailWithPassword: [{ex.Message}]"
                return None
        }

    let Exists (email: string): Task<bool> =
        task {
            let existsCmd =
                count {
                    collection UsersColName
                    query {| email = email |}
                }

            let! result = database.Value.RunCommandAsync<CountResult>(JsonCommand existsCmd)

            return result.n > 0
        }

    let TryCreate (user: {| name: string
                            lastName: string
                            email: string
                            password: string |})
                  : Task<Option<User>> =
        task {
            let! result =
                database.Value.RunCommandAsync<InsertResult>(JsonCommand(insert UsersColName { documents [ user ] }))

            if result.n > 0 && result.ok = 1.0 then return! TryFindByEmail user.email else return None
        }
