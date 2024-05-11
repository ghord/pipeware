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
    public class RoutingTests
    {
        class MyRoutingRequestContext : IRequestContext, IRequestPathFeature, IRouteValuesFeature, IFailureFeature
        {
            public MyRoutingRequestContext(string requestPath, IServiceProvider serviceProvider)
            {
                Path = requestPath;
                PathBase = string.Empty;
                QueryString = string.Empty;
                RouteValues = new RouteValueDictionary();

                Features = new FeatureCollection();
                Features.Set<IRequestPathFeature>(this);
                Features.Set<IRouteValuesFeature>(this);
                Features.Set<IFailureFeature>(this);

                RequestServices = serviceProvider;
            }

            public IFeatureCollection Features { get; }
            public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
            public IServiceProvider RequestServices { get; }
            public PathString PathBase { get; set; }
            public PathString Path { get; set; }
            public string QueryString { get; set; }
            public RouteValueDictionary RouteValues { get; set; }
            public bool IsFailure { get; set; }
            public Exception? Exception { get; set; }
        }

        [TestMethod]
        public async Task ShouldRouteRequest()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddRoutingCore<MyRoutingRequestContext>();
            serviceCollection.AddLogging();
            serviceCollection.TryAddSingleton(sp => new DiagnosticListener("Pipeware"));
            serviceCollection.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var builder = new PipelineBuilder<MyRoutingRequestContext>(serviceProvider);

            builder.UseRouting();
            builder.UseEndpoints(endpoints =>
            {
                endpoints.Map("/test", ctx =>
                {
                    ctx.Items["response"] = "testEndpoint";

                    return Task.CompletedTask;
                });
            });

            var pipeline = builder.Build();

            var request = new MyRoutingRequestContext("/test", serviceProvider.CreateScope().ServiceProvider);

            await pipeline(request);

            Assert.AreEqual("testEndpoint", request.Items["response"]);
        }

        [TestMethod]
        public async Task ShouldRouteRequestWithRouteValue()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddRoutingCore<MyRoutingRequestContext>();
            serviceCollection.AddLogging();
            serviceCollection.TryAddSingleton(sp => new DiagnosticListener("Pipeware"));
            serviceCollection.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var builder = new PipelineBuilder<MyRoutingRequestContext>(serviceProvider);

            builder.UseRouting();
            builder.UseEndpoints(endpoints =>
            {
                endpoints.Map("/test/{name}", (MyRoutingRequestContext ctx, string name) =>
                {
                    ctx.Items["response"] = $"Hello, {name}!";

                    return Task.CompletedTask;
                });
            });

            var pipeline = builder.Build();

            var request = new MyRoutingRequestContext("/test/world", serviceProvider.CreateScope().ServiceProvider);

            await pipeline(request);

            Assert.AreEqual("Hello, world!", request.Items["response"]);
        }

        [TestMethod]
        public async Task ShouldRouteRequestBetweenEndpoints()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddRoutingCore<MyRoutingRequestContext>();
            serviceCollection.AddLogging();
            serviceCollection.TryAddSingleton(sp => new DiagnosticListener("Pipeware"));
            serviceCollection.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var builder = new PipelineBuilder<MyRoutingRequestContext>(serviceProvider);

            builder.UseRouting();
            builder.UseEndpoints(_ => { });

            builder.Map("/test", ctx =>
            {
                ctx.Items["response"] = "testEndpoint";

                return Task.CompletedTask;
            });

            builder.Map("/test2", ctx =>
            {
                ctx.Items["response"] = "testEndpoint2";

                return Task.CompletedTask;
            });

            var pipeline = builder.Build();

            var request = new MyRoutingRequestContext("/test", serviceProvider.CreateScope().ServiceProvider);

            await pipeline(request);


            Assert.AreEqual("testEndpoint", request.Items["response"]);
            var request2 = new MyRoutingRequestContext("/test2", serviceProvider.CreateScope().ServiceProvider);

            await pipeline(request2);

            Assert.AreEqual("testEndpoint2", request2.Items["response"]);
        }

    }
}
