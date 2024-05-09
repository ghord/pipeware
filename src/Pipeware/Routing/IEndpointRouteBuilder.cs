using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Routing
{
    /// <inheritdoc />
    public interface IEndpointRouteBuilder<TRequestContext, TSelf> : IEndpointRouteBuilder<TRequestContext>
        where TSelf : IEndpointRouteBuilder<TRequestContext, TSelf>
        where TRequestContext : class, IRequestContext
    {
    }
}
