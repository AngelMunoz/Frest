module Frest.Domain

open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes

[<BsonIgnoreExtraElements>]
type User =
    { _id: ObjectId
      name: string
      lastName: string
      email: string }

type Place =
    { _id: ObjectId
      name: string
      owner: string
      lat: float
      lon: float }

[<Struct>]
type PaginatedResult<'T> = { count: int; items: seq<'T> }

[<Struct>]
type PaginationParams = { page: int; limit: int }


type ObjectIdFilter = { ``$oid``: ObjectId }
type PlaceFilterById = { _id: ObjectIdFilter; owner: string }

type SavePlaceDefinition =
    { name: string
      owner: string
      lat: float
      lon: float }

type PlaceUpdateDefinition =
    { _id: ObjectIdFilter
      name: string
      owner: string
      lat: float
      lon: float }

    static member FromPlace(place: Place) =
        { _id = { ``$oid`` = place._id }
          name = place.name
          owner = place.owner
          lat = place.lat
          lon = place.lon }


type SignupPayload =
    { name: string
      lastName: string
      email: string
      password: string }

type LoginPayload = { email: string; password: string }
