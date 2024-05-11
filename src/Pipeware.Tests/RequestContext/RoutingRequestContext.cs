using Microsoft.Extensions.DependencyInjection;
using Pipeware.Features;
using Pipeware.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.Tests.RequestContext
{
    public class RoutingRequestContext : IRequestContext, IRequestPathFeature, IRouteValuesFeature, IResponseObjectFeature, IFailureFeature, IRequestBodyFeature
    {
        private QueryFeature _queryFeature;

        public RoutingRequestContext(string path, IServiceScope serviceScope)
        {
            PathBase = string.Empty;
            Path = new PathString(path);
            RouteValues = new RouteValueDictionary();
            Features = new FeatureCollection();
            RequestServices = serviceScope.ServiceProvider;
            QueryString = string.Empty;

            Features.Set<IRequestPathFeature>(this);
            Features.Set<IQueryFeature>(_queryFeature = new QueryFeature(Features));
            Features.Set<IRouteValuesFeature>(this);
            Features.Set<IResponseObjectFeature>(this);
            Features.Set<IFailureFeature>(this);
            Features.Set<IRequestBodyFeature>(this);    
        }

        public IFeatureCollection Features { get; }

        public IDictionary<string, object?> Items => throw new NotImplementedException();

        public IServiceProvider RequestServices { get; }

        public PathString PathBase { get; set; }

        public object? Result { get; set; }

        public string QueryString { get; set; }

        public PathString Path { get; set; }

        public object? Body { get; set; }

        public RouteValueDictionary RouteValues { get; set; }
        
        public bool IsFailure { get; set; } 
        public Exception? Exception { get; set; }

        public Task<object?> GetBodyAsync(Type bodyType)
        {
            return Task.FromResult(Body);
        }

        public Task SetResultAsync(object? result)
        {
            Result = result;
            return Task.CompletedTask;
        }
    }
}
