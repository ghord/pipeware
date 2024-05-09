using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pipeware.Builder;
using Pipeware.Features;
using Pipeware.Routing;
using Pipeware.Tests.RequestContext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Tests;

[TestClass]
public class RequestDelegateFactoryTests : RoutingTestsBase
{
    [TestMethod]
    public async Task ShouldRouteRequest()
    {
        var pipeline = CreatePipeline(builder =>
        {
            builder.Map("/test/{name}", (string name, RoutingRequestContext context) =>
            {
                context.Result = $"Hello, {name}!";
            });
        }, out var services);

        var request = new RoutingRequestContext("/test/testName", services.CreateScope());

        await pipeline(request);

        Assert.IsFalse(request.IsFailure);
        Assert.AreEqual("Hello, testName!", request.Result);
    }

    [TestMethod]
    public async Task ShouldRouteRequestWithStringResult()
    {
        var pipeline = CreatePipeline(builder =>
        {
            builder.Map("/test/{name}", (string name) =>
            {
                return $"Hello, {name}!";
            });
        }, out var services);

        var request = new RoutingRequestContext("/test/testName", services.CreateScope());

        await pipeline(request);

        Assert.IsFalse(request.IsFailure);

        Assert.AreEqual("Hello, testName!", request.Result);
    }

    [TestMethod]
    public async Task ShouldRouteRequestWithAsyncStringResult()
    {
        var pipeline = CreatePipeline(builder =>
        {
            builder.Map("/test/{name}", async (string name) =>
            {
                await Task.Yield();

                return $"Hello, {name}!";
            });
        }, out var services);

        var request = new RoutingRequestContext("/test/testName", services.CreateScope());

        await pipeline(request);

        Assert.IsFalse(request.IsFailure);

        Assert.AreEqual("Hello, testName!", request.Result);
    }

    [TestMethod]
    public async Task ShouldRouteRequestAndBindFromQuery()
    {
        var pipeline = CreatePipeline(builder =>
        {
            builder.Map("/test/{name}", async (string name, [FromQuery] string surname) =>
            {
                await Task.Yield();

                return $"Hello, {name}!";
            });
        }, out var services);

        var request = new RoutingRequestContext("/test/testName", services.CreateScope())
        {
            QueryString = "?surname=testSurname"
        };

        await pipeline(request);

        Assert.IsFalse(request.IsFailure);

        Assert.AreEqual("Hello, testName!", request.Result);
    }

    class MockData
    {
        public required string Name { get; set; }
    }

    [TestMethod]
    public async Task ShoutRouteRequestAndBindBody()
    {
        var pipeline = CreatePipeline(builder =>
        {
            builder.Map("/test/{name}", (string name, MockData mockData) =>
            {
                return $"Hello, {mockData.Name}!";
            });
        }, out var services);

        var request = new RoutingRequestContext("/test/testName", services.CreateScope())
        {
            Body = new MockData { Name = "testName" }
        };

        await pipeline(request);


        Assert.IsFalse(request.IsFailure);
        Assert.AreEqual("Hello, testName!", request.Result);
    }

    [TestMethod]
    public async Task ShouldRouteRequestAndBindBodyNullable()
    {
        var pipeline = CreatePipeline(builder =>
        {
            builder.Map("/test/{name}", (string name, MockData? mockData) =>
            {
                return $"Hello, {mockData?.Name}!";
            });
        }, out var services);

        var request = new RoutingRequestContext("/test/testName", services.CreateScope())
        {

        };

        await pipeline(request);

        Assert.IsFalse(request.IsFailure);
        Assert.AreEqual("Hello, !", request.Result);
    }

    [TestMethod]
    public async Task ShouldRouteRequestAndFailOnNullBody()
    {
        var pipeline = CreatePipeline(builder =>
        {
            builder.Map("/test/{name}", (string name, MockData mockData) =>
            {
                return $"Hello, {mockData.Name}!";
            });
        }, services =>
        {
            services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
        },
        out var services);

        var request = new RoutingRequestContext("/test/testName", services.CreateScope())
        {

        };

        await Assert.ThrowsExceptionAsync<BadRequestException>(async delegate
        {
            await pipeline(request);
        });
    }

    class ThrowingRequestBodyFeature : IRequestBodyFeature
    {
        private bool _preventRethrow;
        private Type _exceptionType;

        public ThrowingRequestBodyFeature(bool preventRethrow, Type exceptionType)
        {
            _preventRethrow = preventRethrow;
            _exceptionType = exceptionType;
        }

        public Task<object?> GetBodyAsync(Type bodyType)
        {
            throw (Exception)Activator.CreateInstance(_exceptionType)!;
        }

        public bool IsBadRequestException(Exception ex, out bool preventRethrow)
        {
            preventRethrow = _preventRethrow;
            return _exceptionType.IsAssignableFrom(ex.GetType());
        }
    }

    [TestMethod]
    public async Task ShouldRouteRequestAndFailOnCustomExceptionBody()
    {
        var pipeline = CreatePipeline(builder =>
        {
            builder.Map("/test", (MockData? mockData) =>
            {
            });

        }, services =>
        {
            services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
        },
        out var services);

        var throwingRequest = new RoutingRequestContext("/test", services.CreateScope());

        throwingRequest.Features.Set<IRequestBodyFeature>(new ThrowingRequestBodyFeature(false, typeof(NotImplementedException)));

        await Assert.ThrowsExceptionAsync<BadRequestException>(async delegate
        {
            await pipeline(throwingRequest);
        });

        var nonThrowingRequest = new RoutingRequestContext("/test", services.CreateScope());

        throwingRequest.Features.Set<IRequestBodyFeature>(new ThrowingRequestBodyFeature(true, typeof(NotImplementedException)));

        await pipeline(nonThrowingRequest);

        Assert.IsFalse(nonThrowingRequest.IsFailure);
    }

 
}
