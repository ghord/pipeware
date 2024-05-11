using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pipeware.Builder;
using Pipeware.Features;

namespace Pipeware.Tests;

[TestClass]
public class SyncMiddlewareTests
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
    public void ShouldEchoRequestWithDelegateMiddleware()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // define pipeline
        var builder = new SyncPipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.Use(r => ctx => { r(ctx); ctx.Response = ctx.Request; });

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    [TestMethod]
    public void ShouldEchoRequestWithInlineDelegateMiddleware()
    {
        // define service provider
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // define pipeline
        var builder = new SyncPipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.Use((ctx, next) =>
        {
            next(ctx);

            ctx.Response = ctx.Request;
        });

        // builds delegate
        var pipeline = builder.Build();

        using var scope = serviceProvider.CreateScope();

        // create context
        var ctx = new MyRequestContext
        {
            // custom payload sent with request
            Request = "test",

            // create scope for dependency injection
            RequestServices = scope.ServiceProvider
        };

        // invoke pipeline
        pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    [TestMethod]
    public void ShouldEchoRequestWithTerminalMiddleware()
    {
        // define service provider
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // define pipeline
        var builder = new SyncPipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.Run(ctx =>
        {
            ctx.Response = ctx.Request;
        });

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    class MyMiddleware : ISyncMiddleware<MyRequestContext>
    {
        public void Invoke(MyRequestContext context, SyncRequestDelegate<MyRequestContext> next)
        {
            next(context);

            context.Response = context.Request;
        }
    }

    [TestMethod]
    public void ShouldEchoRequestWithIMiddleware()
    {
        // service provider
        var services = new ServiceCollection();

        services.AddScoped<ISyncMiddlewareFactory<MyRequestContext>, SyncMiddlewareFactory<MyRequestContext>>();
        services.AddScoped<MyMiddleware>();

        var serviceProvider = services.BuildServiceProvider();

        // define pipeline
        var builder = new SyncPipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.UseMiddleware(typeof(MyMiddleware));

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    [TestMethod]
    public void ShouldEchoRequestWithIMiddlewareGeneric()
    {
        // service provider
        var services = new ServiceCollection();

        services.AddScoped<ISyncMiddlewareFactory<MyRequestContext>, SyncMiddlewareFactory<MyRequestContext>>();
        services.AddScoped<MyMiddleware>();

        var serviceProvider = services.BuildServiceProvider();

        // define pipeline
        var builder = new SyncPipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined echo middleware, which always sets response to request value
        builder.UseMiddleware(typeof(MyMiddleware));

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        pipeline(ctx);

        Assert.AreEqual("test", ctx.Response);
    }

    class MyConventionMiddleware
    {
        private SyncRequestDelegate<MyRequestContext> _next;
        private string _prefix;

        public MyConventionMiddleware(SyncRequestDelegate<MyRequestContext> next, string prefix)
        {
            _next = next;
            _prefix = prefix;
        }

        public void Invoke(MyRequestContext context)
        {
            _next(context);

            context.Response = _prefix + context.Request;
        }
    }


    [TestMethod]
    public void ShouldEchoRequestWithConventionMiddleware()
    {
        // service provider
        var services = new ServiceCollection();

        services.AddScoped<ISyncMiddlewareFactory<MyRequestContext>, SyncMiddlewareFactory<MyRequestContext>>();
        services.AddScoped<MyConventionMiddleware>();

        var serviceProvider = services.BuildServiceProvider();

        // define pipeline
        var builder = new SyncPipelineBuilder<MyRequestContext>(serviceProvider);

        // install user defined convention echo middleware, which always sets response to request value with a prefix
        builder.UseMiddleware(typeof(MyConventionMiddleware), "prefix_");

        // builds delegate
        var pipeline = builder.Build();

        // create context
        var ctx = new MyRequestContext { Request = "test", RequestServices = serviceProvider.CreateScope().ServiceProvider };

        // invoke pipeline
        pipeline(ctx);

        Assert.AreEqual("prefix_test", ctx.Response);
    }
}