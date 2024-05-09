using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pipeware.Builder;
using Pipeware.Features;

namespace Pipeware.Tests;

[TestClass]
public class MiddlewareTests
{
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

    [TestMethod]
    public async Task ShouldEchoRequestWithDelegateMiddleware()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // define pipeline
        var builder = new PipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.Use(r => async ctx => { await r(ctx); ctx.Response = ctx.Request; });

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        await pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    [TestMethod]
    public async Task ShouldEchoRequestWithInlineDelegateMiddleware()
    {
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

        // builds delegate
        var pipeline = builder.Build();

        await using var scope = serviceProvider.CreateAsyncScope();

        // create context
        var ctx = new MyRequestContext
        {
            // custom payload sent with request
            Request = "test",

            // create scope for dependency injection
            RequestServices = scope.ServiceProvider
        };

        // invoke pipeline
        await pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    [TestMethod]
    public async Task ShouldEchoRequestWithTerminalMiddleware()
    {
        // define service provider
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // define pipeline
        var builder = new PipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.Run(async ctx =>
        {
            await Task.Yield();

            ctx.Response = ctx.Request;
        });

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        await pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    class MyMiddleware : IMiddleware<MyRequestContext>
    {
        public async Task InvokeAsync(MyRequestContext context, RequestDelegate<MyRequestContext> next)
        {
            await next(context);

            context.Response = context.Request;
        }
    }

    [TestMethod]
    public async Task ShouldEchoRequestWithIMiddleware()
    {
        // service provider
        var services = new ServiceCollection();

        services.AddScoped<IMiddlewareFactory<MyRequestContext>, MiddlewareFactory<MyRequestContext>>();
        services.AddScoped<MyMiddleware>();

        var serviceProvider = services.BuildServiceProvider();

        // define pipeline
        var builder = new PipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.UseMiddleware(typeof(MyMiddleware));

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        await pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    [TestMethod]
    public async Task ShouldEchoRequestWithIMiddlewareGeneric()
    {
        // service provider
        var services = new ServiceCollection();

        services.AddScoped<IMiddlewareFactory<MyRequestContext>, MiddlewareFactory<MyRequestContext>>();
        services.AddScoped<MyMiddleware>();

        var serviceProvider = services.BuildServiceProvider();

        // define pipeline
        var builder = new PipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.UseMiddleware(typeof(MyMiddleware));

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        await pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    class MyConventionMiddleware
    {
        private RequestDelegate<MyRequestContext> _next;
        private string _prefix;

        public MyConventionMiddleware(RequestDelegate<MyRequestContext> next, string prefix)
        {
            _next = next;
            _prefix = prefix;
        }

        public async Task InvokeAsync(MyRequestContext context)
        {
            await _next(context);

            context.Response = _prefix + context.Request;
        }
    }


    [TestMethod]
    public async Task ShouldEchoRequestWithConventionMiddleware()
    {
        // service provider
        var services = new ServiceCollection();

        services.AddScoped<IMiddlewareFactory<MyRequestContext>, MiddlewareFactory<MyRequestContext>>();
        services.AddScoped<MyConventionMiddleware>();

        var serviceProvider = services.BuildServiceProvider();

        // define pipeline
        var builder = new PipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined convention echo middleware, which always sets response to request value with a prefix
        builder.UseMiddleware(typeof(MyConventionMiddleware), "prefix_");

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        await pipeline(ctx);

        Assert.AreEqual("prefix_test", ctx.Response);
    }
}