using Pipeware.Tests.RequestContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pipeware.Builder;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Pipeware.Tests
{
    [TestClass]
    public class EndpointFilteringTests : RoutingTestsBase
    {
        [TestMethod]
        public async Task ShouldAddEndpointFilterDelegate()
        {
            var pipeline = CreatePipeline(builder =>
            {
                builder.Map("/test/{value:int}", (int value) =>
                {
                    return "testResult" + value;

                }).AddEndpointFilter(async (context, next) =>
                {
                    if (context.GetArgument<int>(0) % 2 == 0)
                    {
                        context.RequestContext.GetResultFailureFeature().IsFailure = true;
                    }

                    return await next(context);
                });
            }, out var serviceProvider);


            var request = new RoutingRequestContext("/test/1", serviceProvider.CreateScope());
            var request2 = new RoutingRequestContext("/test/2", serviceProvider.CreateScope());

            await pipeline(request);

            Assert.AreEqual("testResult1", request.Result);

            await pipeline(request2);

            Assert.IsTrue(request2.IsFailure);
            Assert.IsNull(request2.Result);
        }

        class MyFilter : IEndpointFilter<RoutingRequestContext>
        {
            public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext<RoutingRequestContext> context, EndpointFilterDelegate<RoutingRequestContext> next)
            {
                if (context.GetArgument<int>(0) % 2 == 0)
                {
                    context.RequestContext.GetResultFailureFeature().IsFailure = true;
                }

                return await next(context);
            }
        }

        [TestMethod]
        public async Task ShouldAddEndpointFilter()
        {
            var pipeline = CreatePipeline(builder =>
            {
                builder.Map("/test/{value:int}", (int value) =>
                {
                    return "testResult" + value;

                }).AddEndpointFilter(typeof(MyFilter));
            }, out var serviceProvider);


            var request = new RoutingRequestContext("/test/1", serviceProvider.CreateScope());
            var request2 = new RoutingRequestContext("/test/2", serviceProvider.CreateScope());

            await pipeline(request);

            Assert.AreEqual("testResult1", request.Result);

            await pipeline(request2);

            Assert.IsTrue(request2.IsFailure);
            Assert.IsNull(request2.Result);
        }

        [TestMethod]
        public async Task ShouldAddEndpointFilterFactory()
        {
            var pipeline = CreatePipeline(builder =>
            {
                builder.Map("/test/{value:int}", (int value) =>
                {
                    return "testResult" + value;

                }).AddEndpointFilterFactory((filterFactoryContext, next) =>
                {
                    var parameters = filterFactoryContext.MethodInfo.GetParameters();
                    if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(int))
                    {
                        return async invocationContext =>
                        {
                            if (invocationContext.GetArgument<int>(0) % 2 == 0)
                            {
                                invocationContext.RequestContext.GetResultFailureFeature().IsFailure = true;
                            }

                            return await next(invocationContext);
                        };
                    }
                    return invocationContext => next(invocationContext);
                });
            }, out var serviceProvider);


            var request = new RoutingRequestContext("/test/1", serviceProvider.CreateScope());
            var request2 = new RoutingRequestContext("/test/2", serviceProvider.CreateScope());

            await pipeline(request);

            Assert.AreEqual("testResult1", request.Result);

            await pipeline(request2);

            Assert.IsTrue(request2.IsFailure);
            Assert.IsNull(request2.Result);
        }

        [TestMethod]
        public async Task ShouldAddEndpointFilterFactoryWithoutRdf()
        {
            var pipeline = CreatePipeline(builder =>
            {
                builder.Map("/test/{value:int}", async ctx =>
                {
                    await Task.Yield();

                    ctx.Result = $"testResult{ctx.RouteValues["value"]}";

                }).AddEndpointFilter(async (context, next) =>
                {
                    if (int.Parse((string)context.RequestContext.RouteValues["value"]!) % 2 == 0)
                    {
                        context.RequestContext.GetResultFailureFeature().IsFailure = true;
                    }

                    return await next(context);
                });
            }, out var serviceProvider);


            var request = new RoutingRequestContext("/test/1", serviceProvider.CreateScope());
            var request2 = new RoutingRequestContext("/test/2", serviceProvider.CreateScope());

            await pipeline(request);

            Assert.AreEqual("testResult1", request.Result);

            await pipeline(request2);

            Assert.IsTrue(request2.IsFailure);
            Assert.IsNull(request2.Result);
        }

        [TestMethod]
        public async Task ShouldShortCircuitPipelineWithFilter()
        {
            var pipeline = CreatePipeline(builder =>
            {
                builder.Map("", () => "testResult").AddEndpointFilter((context, next) =>
                {
                    return ValueTask.FromResult<object?>("filterResult");
                });
            }, out var serviceProvider);


            var ctx = new RoutingRequestContext("", serviceProvider.CreateScope());

            await pipeline(ctx);

            Assert.AreEqual("filterResult", ctx.Result);
        }

        [TestMethod]
        public async Task ShouldWorkWithEmptyFilterFactory()
        {
            var pipeline = CreatePipeline(builder =>
            {
                builder.Map("", async ctx =>
                {
                    ctx.Result = "testResult";
                    
                    await Task.Yield();

                });  
            }, out var serviceProvider);

            var ctx = new RoutingRequestContext("", serviceProvider.CreateScope());

            await pipeline(ctx);

            Assert.IsFalse(ctx.IsFailure);
            Assert.AreEqual("testResult", ctx.Result);
        }
    }
}
