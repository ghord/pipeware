using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pipeware.Builder;
using Pipeware.Tests.RequestContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Tests
{
    [TestClass]
    public class MapGroupTests : RoutingTestsBase
    {
        [TestMethod]
        public async Task ShouldRouteFromGroup()
        {
            var pipeline = CreatePipeline(builder =>
            {
                var group = builder.MapGroup("/group");
                group.Map("/test", ctx =>
                {
                    ctx.Result = "testEndpoint";

                    return Task.CompletedTask;
                });

                group.Map("/test2", ctx =>
                {
                    ctx.Result = "testEndpoint2";

                    return Task.CompletedTask;
                });

            }, out var serviceProvider);

            var request = new RoutingRequestContext("/group/test", serviceProvider.CreateScope());
            var request2 = new RoutingRequestContext("/group/test2", serviceProvider.CreateScope());

            await pipeline(request);

            Assert.AreSame(request.Result, "testEndpoint");

            await pipeline(request2);

            Assert.AreSame(request2.Result, "testEndpoint2");
        }
    }
}
