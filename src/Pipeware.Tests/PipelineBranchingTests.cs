using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pipeware.Builder;
using Pipeware.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Tests;

class MyRequestContext : IRequestContext
{
    public required int BranchId { get; init; }

    public bool Success { get; set; }

    public IFeatureCollection Features { get; } = new FeatureCollection();

    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();

    public required IServiceProvider RequestServices { get; init; }
}

class MyBranchingMiddleware
{
    private RequestDelegate<MyRequestContext> _next;
    private RequestDelegate<MyRequestContext> _branch;
    private int _branchId;

    public MyBranchingMiddleware(RequestDelegate<MyRequestContext> next, int branchId, RequestDelegate<MyRequestContext> branch)
    {
        _next = next;
        _branch = branch;
        _branchId = branchId;
    }

    public async Task InvokeAsync(MyRequestContext context)
    {
        if (context.BranchId == _branchId)
        {
            await _branch(context);
        }
        else
        {
            await _next(context);
        }
    }
}
static class MyBranchingExtensions
{
    public static IPipelineBuilder<MyRequestContext> Map(this IPipelineBuilder<MyRequestContext> builder, int branchId, Action<IPipelineBuilder<MyRequestContext>> configuration)
    {
        var branchBuilder = builder.New();
        configuration(branchBuilder);
        var branch = branchBuilder.Build();

        return builder.Use(next => new MyBranchingMiddleware(next, branchId, branch).InvokeAsync);
    }
}

[TestClass]
public class PipelineBranchingTests
{
    [TestMethod]
    public async Task ShouldBranchPipeline()
    {
        var services = new ServiceCollection();

        var serviceProvider = services.BuildServiceProvider();

        var builder = new PipelineBuilder<MyRequestContext>(serviceProvider);

        // separate branch is added to pipeline. This branch only triggers on MyRequestContext.BranchId = 1
        builder.Map(1, app => app.Run(ctx => { ctx.Success = true; return Task.CompletedTask; }));

        var pipeline = builder.Build();

        var ctx1 = new MyRequestContext { BranchId = 1, RequestServices = serviceProvider.CreateScope().ServiceProvider };
        var ctx2 = new MyRequestContext { BranchId = 2, RequestServices = serviceProvider.CreateScope().ServiceProvider };

        await pipeline(ctx1);

        Assert.AreEqual(true, ctx1.Success);

        await pipeline(ctx2);

        Assert.AreEqual(false, ctx2.Success);
    }
}
