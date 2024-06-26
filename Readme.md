![Logo](art/icon.png)

# Pipeware

**Pipeware** is a .Net library that enables developers to host custom AspNetCore style request pipelines using middlewares, dependency injection, and routing 

## Motivation

`AspNetCore`'s request pipeline is an elegant solution for a number of challenges:

- Building composable applications using [chain-of-responsibility pattern](https://en.wikipedia.org/wiki/Chain-of-responsibility_pattern) through [Middlewares](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0). 
- `IFeatureCollection` abstraction provides a clean [composite pattern](https://en.wikipedia.org/wiki/Composite_pattern) implementation for extending `HttpContext`.
- [Minimal API Routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-8.0) allows developers to divide complex applications into distinct, well defined parts using `Endpoints`, with `RouteDelegateFactory` supporting complex parameter bindings.
- Extensive use of `IServiceProvider` provides easy to use [Inversion of Control](https://en.wikipedia.org/wiki/Inversion_of_control).

Unfortunately, it is very closely tied to `http` stack, mixing pipeline abstractions such as Middlewares and Features with `http` abstractions such as status codes, request bodies, headers, http methods and many more. Often, it is desirable to have an abstract pipeline for handling different kinds of requests without using `http` stack. 

Additionally, `AspNetCore` pipelines are asynchronous, making them unusable from synchronous contexts, such as window message handlers or synchronous interface member implementations.

**Minimal API routing** is another very powerful abstraction tied closely to `http` stack in **AspNetCore**, and it should be possible to use it as a standalone component. In fact,  **AspNetCore** team had to use `#ifdef` directives in sources to use duplicate routing for razor components, showing that routing implementation is too tied to the framework itself and not abstract. It is also coupled with `Json` serialization, which is only convenient when request and response bodies are passed around as plaintext.

For those reasons, **Pipeware** introduces a way to define generic async and sync pipelines with user provided `IRequestContext` implementation, including dependency injection, middleware support and routing (in async pipelines only) for user defined requests.

## Implementation

### Source Import

Most of the code in this library was automatically imported from https://github.com/dotnet/aspnetcore by custom, roslyn based `Pipeware.SourceImport` tool. All files in `src/Pipeware/SourceImport` are taken directly from `release/8.0` branch with transformations applied to change namespaces and make the code generic with respect to `TRequestContext`. Transformations are described in `src/Pipeware/SourceImport/SourceImport.json` and should be self-explanatory.
 
The intention is to be able to easily upgrade this library with new features introduced by **AspNetCore** team by re-running import tool when new version is released.

### Core Pipeware abstractions

`IRequestContext` and `PipelineBuilder<TRequestContext>` are core abstractions and entry point for defining pipelines.

|AspNetCore abstraction|Pipeware abstraction|Remarks
|-|-|-
|`HttpContext`| `IRequestContext`<br>`TRequestContext : IRequestContext`| Pipeware does not provide an implementation of HttpContext out of the box. User of the library has to define its own implementation for use case
|`RequestDelegate` | `RequestDelegate<TRequestContext>`<br>`SyncRequestDelegate<TRequestContext>` |  Returned from `(Sync)PipelineBuilder<TRequestContext>.Build()`. Represents a fully built request pipeline, that can be invoked by passing instance of `TRequestContext`. Both sync and async versions are supported.
|`IApplicationBuilder`| `IPipelineBuilder<TRequestContext>`<br>`ISyncPipelineBuilder<TRequestContext>` | Used for registering middlewares. Implemented by `PipelineBuilder<TRequestContext>` and `SyncPipelineBuilder<TRequestContext>`. Both sync and async versions are supported.
|`IEndpointRouteBuilder` | `IEndpointRouteBuilder<TRequestContext>` | Used for mapping endpoint routes. Implemented by `PipelineBuilder<TRequest>`.<br>Only async version is supported. |

### Supported functionalities, imported from AspNetCore

|Functionality|Remarks
|-|-
|[Middlewares](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0)| Delegate middlewares, `IMiddleware<TRequestContext>`, `ISyncMiddleware<TRequestContext>` convention based middlewares are supported, with both sync and async versions. <br><br>Using `I(Sync)Middleware<TRequestContext>` requires registration of scoped `I(Sync)MiddlewareFactory<TRequestContext>` with default `(Sync)MiddlewareFactory<TRequestContext>` implementation available.<br><br>Using `I(Sync)Middleware<TRequestContext>` or convention based middlewares requires registration of scoped middlewares with dependency injection.
|[Route Handlers](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-8.0)|Excluded `http` specific features:<br><li>http verbs (`MapGet`/`MapPost`/...)*<br><li>`ShortCircuit(StatusCode)`<br><br>\*-*can be implemented using `IParameterPolicy`*
|[Parameter Binding](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-8.0)|Excluded `http` specific features:<br><li>`[FromForm]`<li>`[FromHeaders]`<li>`Stream`/`PipeReader` binding

### Functionality removed compared to AspNetCore

All functionalities directly tied to `http` stack were removed during import of the code

|Functionality|Remarks
|-|-
|`Authorization`<br>`Authentication`| For now, Authorization and Authentication are unsupported
|`StatusCodes` | Requests do not have to use `http` status codes to communicate results. Replaced by `IsFailure` flag.
|`Headers` | For now, Headers are unsupported 
|`FormData` | Forms are tied to `http` and are unsupported
|`Host`, `Scheme`, | Host and Scheme are unsupported in routing. Link generation will only generate full urls with caller provided `Host` and `Scheme`.
|`ContentType`| Request Body/Response for `IRequestContext` can be any `object` instance, not just byte stream as in http. 
|automatic `Json` serialization | As response of a request can be any object, there is no longer a need for `Json` serialization of response body or deserialization of request body.

## Implementing IRequestContext

`IRequestContext` is an abstraction over request. Instead of using single fit-all request context, consumers of library are required to provide their own implementation on `IRequestContext` with required `IFeatureCollection` support.

```c#
interface IRequestContext
{
    IFeatureCollection Features { get; }

    IDictionary<string, object?> Items { get; }

    IServiceProvider RequestServices { get; }
}
```

TODO: Details on how to implement request context correctly.

## Roadmap

### 1.0.0

- `IPipelineBuilder<TRequestContext>` implementation **(in preview1)**
- Middleware support with pipeline branching **(in preview1)**
- Routing middleware support **(in preview1)**
- `RequestDelegateFactory` implemented **(in preview1)**
- Filters support **(in preview2)**
- `MapGroup`, `MapWhen` support **(in preview2)**
- Remove generic parameters from unnecessary types **(in preview2)**
- Move to using `Microsoft.CodeAnalysis.PublicApi` **(in preview2)**
- Share the same `IServiceProvider` across multiple pipelines **(in preview2)**
- Remove all mentions of httpContext from public API **(in preview2)**
- Trimming annotated **(in preview3)**
- Link generation **(in preview3)**
- Update generic extension methods to use CRTP **(in preview3)**
- Support synchronous pipelines **(in preview4)**
- Remove all mentions of http from comments on public methods
- API review of new features
- 100% test coverage of handwritten code 
- `RequestDelegateFactory` parameter binding extensibility

### 1.1.0

- `RequestDelegateGenerator` implemented
- AOT compatible
- Analyzers for routing
- ...

### Future (consideration)

- Sync routing

## Examples

### 1. Basic async pipeline

This example shows how to define simple pipeline for requests which echo middleware.

First, we need to define basic `IRequestContext` implementation

```c#
// user defined request context
class MyRequestContext : IRequestContext
{
    // user defined properties
    public required string Request { get; init; }
    public string? Response { get; set; }

    // IRequestContext properties properties
    public required IServiceProvider RequestServices { get; init; }
    public IFeatureCollection Features { get; } = new FeatureCollection();
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}
```

Next, lets define simple async pipeline with a middleware that echoes request payload as a response:


```c#
// define service provider
var serviceProvider = new ServiceCollection().BuildServiceProvider();

// define pipeline
var builder = new PipelineBuilder<MyRequestContext>(serviceProvider);

// install user defined echo middleware, which always sets response to request value
builder.Use(async (ctx, next) =>
{
    await next(ctx);

    ctx.Response = ctx.Request;
});

// builds pipeline as delegate
var pipeline = builder.Build();
```

Now, we can invoke the pipeline as follows:

```c#

// create scope for dependency injection associated with request
using var scope = serviceProvider.CreateScope();

// create request context with payload
var ctx = new MyRequestContext
{
    // custom payload sent with request
    Request = "test",

    // pass the scope for dependency injection resolution
    RequestServices = scope.ServiceProvider
};

// invoke pipeline
await pipeline(ctx);

// check if echo middleware works
Assert.AreEqual("test", ctx.Response);
```

### 2. Basic sync pipeline

Using the same `MyRequestContext`, we can define a sync pipeline:

```c#
// define service provider
var serviceProvider = new ServiceCollection().BuildServiceProvider();

// define pipeline (notice SyncPipelineBuilder usage)
var builder = new SyncPipelineBuilder<MyRequestContext>(serviceProvider);

// install user defined echo middleware, which always sets response to request value
builder.Use((ctx, next) =>
{
    // synchronous invocation of next
    next(ctx);

    ctx.Response = ctx.Request;
});

// builds pipeline as delegate
var pipeline = builder.Build();
```

Now, we can invoke the pipeline as follows:

```c#

// create scope for dependency injection associated with request
using var scope = serviceProvider.CreateScope();

// create request context with payload
var ctx = new MyRequestContext
{
    // custom payload sent with request
    Request = "test",

    // pass the scope for dependency injection resolution
    RequestServices = scope.ServiceProvider
};

// invoke pipeline, synchronously
pipeline(ctx);

// check if echo middleware works
Assert.AreEqual("test", ctx.Response);
```

### 3. Basic routing example

Basic example of routing pipeline can be found [in unit tests](src/Pipeware.Tests/RoutingTests.cs).


