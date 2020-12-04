[falco]: https://
[mondocks]: https://

# Frest

This is a sample API built with [Falco], it uses mongodb as the database via [Mondocks]

## Routes

```fsharp
let endpoints =
    [ post Urls.``/auth/login`` Value.Controller.login
      post Urls.``/auth/signup`` Value.Controller.signup
      get Urls.``/api/me`` Value.Controller.me
      get Urls.``/api/places`` Value.Controller.getPlaces
      post Urls.``/api/places`` Value.Controller.addPlaces
      put Urls.``/api/places/id`` Value.Controller.updatePlaces
      delete Urls.``/api/places/id`` Value.Controller.deletePlaces ]
```

### Organization

While names are almost meaningless to me I followed the Falco templates's rest default structure with a slight change of names.

- Domain

  includes most of the base types there is to work with

- Provider

  includes database access

- Common

  utility functions that can be used accross models/controllers

- Value

  Includes models and controllers along with its behaviors

- Program

  definition of routes and the server's configuration

Basically as the nature of F# this is a top-down project which includes a Restful API with some protected routes as well

`Frest.Web` includes a lit-html frontend
