namespace Pipeware.Builder
{
    /// <inheritdoc />
    public interface IPipelineBuilder<TRequestContext, TSelf> : IPipelineBuilder<TRequestContext>
        where TSelf : IPipelineBuilder<TRequestContext, TSelf>
        where TRequestContext : class, IRequestContext
    {

    }
}
