using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pipeware.Builder;
using Pipeware.Routing;
using Pipeware.Tests.RequestContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Tests
{
    [TestClass]
    public class LinkGenerationTests : RoutingTestsBase
    {
        [TestMethod]
        public async Task ShouldGenerateLinkWithRouteValues()
        {
            var pipeline = CreatePipeline(app =>
            {
                app.Map("/hello/{name}", (string name) => "Hello {name}!")
                   .WithName("hi");

                app.Map("/", (LinkGenerator<RoutingRequestContext> linker) =>
                    $"{linker.GetPathByName("hi", new { Name = "test" })}");
            }, out var services);

            var request = new RoutingRequestContext("/", services.CreateScope());

            await pipeline(request);

            Assert.AreEqual("/hello/test", request.Result);
        }

        [TestMethod]
        public void ShouldGenerateLink()
        {
            var pipeline = CreatePipeline(app =>
            {
                app.Map("/hello/{name}", (string name) => "Hello {name}!")
                   .WithName("hi");
            }, out var services);

            var generator = services.GetRequiredService<LinkGenerator<RoutingRequestContext>>();

            var path = generator.GetPathByName("hi", new { Name = "test" });

            Assert.AreEqual("/hello/test", path);
        }
    }
}
