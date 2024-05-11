namespace Pipeware.Builder
{
    /// <inheritdoc />
    public interface ISyncPipelineBuilder<TRequestContext, TSelf> : ISyncPipelineBuilder<TRequestContext>
        where TSelf : ISyncPipelineBuilder<TRequestContext, TSelf>
        where TRequestContext : class, IRequestContext
    {

    }
}
