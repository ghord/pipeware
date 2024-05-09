using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pipeware.Builder;
using Pipeware.Features;
using Pipeware.Routing;
using Pipeware.Routing.Patterns;
using Pipeware.Tests.RequestContext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Tests
{
    [TestClass]
    public class RoutingTests : RoutingTestsBase
    {
        [TestMethod]
        public async Task ShouldRouteRequest()
        {
            var pipeline = CreatePipeline(builder =>
            {
                builder.Map(RoutePatternFactory.Parse("/test"), (RoutingRequestContext ctx) =>
                {
                    ctx.Result = "testEndpoint";

                    return Task.CompletedTask;
                });

            }, out var serviceProvider);

            var request = new RoutingRequestContext("/test", serviceProvider.CreateScope());
            
            await pipeline(request);

            Assert.AreEqual("testEndpoint", request.Result);
        }

        [TestMethod]
        public async Task ShouldRouteRequestBetweenEndpoints()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddRoutingCore<RoutingRequestContext>();
            serviceCollection.AddLogging();
            serviceCollection.TryAddSingleton(sp => new DiagnosticListener("Pipeware"));
            serviceCollection.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var builder = new PipelineBuilder<RoutingRequestContext>(serviceProvider);

            builder.UseRouting();
            builder.UseEndpoints(_ => { });

            builder.Map("/test", ctx =>
            {
                ctx.Result = "testEndpoint";

                return Task.CompletedTask;
            });

            builder.Map("/test2", ctx =>
            {
                ctx.Result = "testEndpoint2";

                return Task.CompletedTask;
            });

            var pipeline = builder.Build();

            var request = new RoutingRequestContext("/test", serviceProvider.CreateScope());

            await pipeline(request);


            Assert.AreEqual("testEndpoint", request.Result);
            var request2 = new RoutingRequestContext("/test2", serviceProvider.CreateScope());

            await pipeline(request2);

            Assert.AreEqual("testEndpoint2", request2.Result);
        }

    }
}
