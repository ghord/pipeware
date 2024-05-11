using Pipeware.Features;
using Pipeware.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Builder;

public class SyncPipelineBuilder<TRequestContext> : ISyncPipelineBuilder<TRequestContext, SyncPipelineBuilder<TRequestContext>> where TRequestContext : class, IRequestContext
{
    private const string PipelineFeaturesKey = "pipeline.Features";
    private const string ApplicationServicesKey = "application.Services";
    private const string DefaultDelegateKey = "pipeline.DefaultDelegate";

    private List<Func<SyncRequestDelegate<TRequestContext>, SyncRequestDelegate<TRequestContext>>> _components = new();

    public SyncPipelineBuilder(IServiceProvider serviceProvider, IFeatureCollection pipelineFeatures)

    {
        Properties = new Dictionary<string, object?>(StringComparer.Ordinal);

        SetProperty(ApplicationServicesKey, serviceProvider);
        SetProperty(PipelineFeaturesKey, pipelineFeatures);
        SetProperty<SyncRequestDelegate<TRequestContext>>(DefaultDelegateKey, static context =>
        {
        });
    }

    public SyncPipelineBuilder(IServiceProvider serviceProvider)
        : this(serviceProvider, new FeatureCollection())
    {

    }

    public IDictionary<string, object?> Properties { get; }

    private SyncPipelineBuilder(SyncPipelineBuilder<TRequestContext> builder)
    {
        Properties = new CopyOnWriteDictionary<string, object?>(builder.Properties, StringComparer.Ordinal);
    }

    public SyncRequestDelegate<TRequestContext> DefaultDelegate
    {
        get => GetProperty<SyncRequestDelegate<TRequestContext>>(DefaultDelegateKey);
        set => SetProperty(DefaultDelegateKey, value);
    }

    public IFeatureCollection PipelineFeatures => GetProperty<IFeatureCollection>(PipelineFeaturesKey);

    public IServiceProvider ApplicationServices
    {
        get => GetProperty<IServiceProvider>(ApplicationServicesKey);
        set => SetProperty(ApplicationServicesKey, value);
    }

    private void SetProperty<T>(string key, T value)
    {
        Properties[key] = value;
    }

    private T GetProperty<T>(string key)
    {
        return (T)Properties[key]!;
    }

    public ISyncPipelineBuilder<TRequestContext> Use(Func<SyncRequestDelegate<TRequestContext>, SyncRequestDelegate<TRequestContext>> middleware)
    {
        _components.Add(middleware);

        return this;
    }

    public ISyncPipelineBuilder<TRequestContext> New()
    {
        return new SyncPipelineBuilder<TRequestContext>(this);
    }

    public SyncRequestDelegate<TRequestContext> Build()
    {
        var app = DefaultDelegate;

        for (var c = _components.Count - 1; c >= 0; c--)
        {
            app = _components[c](app);
        }

        return app;
    }
}
