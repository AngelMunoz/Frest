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
