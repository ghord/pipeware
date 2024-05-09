using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Builder
{
    /// <inheritdoc />
    public interface IEndpointConventionBuilder<TRequestContext, TSelf> : IEndpointConventionBuilder<TRequestContext>
        where TSelf : IEndpointConventionBuilder<TRequestContext, TSelf>
        where TRequestContext : class, IRequestContext
    {
    }
}
