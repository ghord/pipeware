using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pipeware.Builder;
using Pipeware.Routing;
using Pipeware.Tests.RequestContext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Tests
{
    public abstract class RoutingTestsBase
    {
        protected RequestDelegate<RoutingRequestContext> CreatePipeline(
             Action<IEndpointRouteBuilder<RoutingRequestContext>> configure,
             out IServiceProvider serviceProvider) => CreatePipeline(configure, _ => { }, out serviceProvider);

        protected RequestDelegate<RoutingRequestContext> CreatePipeline(
            Action<IEndpointRouteBuilder<RoutingRequestContext>> configure,
            Action<ServiceCollection> configureServices,
            out IServiceProvider serviceProvider)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddRoutingCore<RoutingRequestContext>();
            serviceCollection.AddLogging();
            serviceCollection.TryAddSingleton(sp => new DiagnosticListener("Pipeware"));
            serviceCollection.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());

            configureServices?.Invoke(serviceCollection);

            serviceProvider = serviceCollection.BuildServiceProvider();

            var builder = new PipelineBuilder<RoutingRequestContext>(serviceProvider);

            builder.UseRouting();
            builder.UseEndpoints(configure);
            

            return builder.Build();
        }
    }
}
