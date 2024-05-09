using Microsoft.Extensions.DependencyInjection;
using Pipeware.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware;

public static partial class EndpointFilterExtensions
{
    /// <summary>
    /// Registers a filter of type <paramref name="filterType"/> onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder{TRequestContext}"/>.</param>
    /// <param name="filterType">The type of the <see cref="IEndpointFilter{TRequestContext}"/> to register.</param>
    /// <returns>A <see cref="RouteHandlerBuilder{TRequestContext}"/> that can be used to further customize the route handler.</returns>
    public static IEndpointConventionBuilder<TRequestContext> AddEndpointFilter<TRequestContext>(this IEndpointConventionBuilder<TRequestContext> builder, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type filterType)
        where TRequestContext : class, IRequestContext
    {
        if (!typeof(IEndpointFilter<TRequestContext>).IsAssignableFrom(filterType))
        {
            throw new InvalidOperationException($"Type '{filterType}' does not implement '{typeof(IEndpointFilter<TRequestContext>)}'.");
        }

        // We call `CreateFactory` twice here since the `CreateFactory` API does not support optional arguments.
        // See https://github.com/dotnet/runtime/issues/67309 for more info.
        ObjectFactory filterFactory;
        try
        {
            filterFactory = ActivatorUtilities.CreateFactory(filterType, new[] { typeof(EndpointFilterFactoryContext) });
        }
        catch (InvalidOperationException)
        {
            filterFactory = ActivatorUtilities.CreateFactory(filterType, Type.EmptyTypes);
        }

        return builder.AddEndpointFilterFactory((routeHandlerContext, next) =>
        {
            var invokeArguments = new[] { routeHandlerContext };
            return (context) =>
            {
                var filter = (IEndpointFilter<TRequestContext>)filterFactory.Invoke(context.RequestContext.RequestServices, invokeArguments);
                return filter.InvokeAsync(context, next);
            };
        });
    }

    /// <summary>
    /// Registers a filter of type <paramref name="filterType"/> onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder{TRequestContext}"/>.</param>
    /// <param name="filterType">The type of the <see cref="IEndpointFilter{TRequestContext}"/> to register.</param>
    /// <returns>A <see cref="RouteHandlerBuilder{TRequestContext}"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddEndpointFilter<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type filterType)
         where TBuilder : IEndpointConventionBuilder<TRequestContext, TBuilder>
         where TRequestContext : class, IRequestContext
    {
        builder.AddEndpointFilter<TRequestContext>(filterType);

        return (TBuilder)builder;
    }

}
