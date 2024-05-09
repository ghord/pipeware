using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport
{
    public static class FormattedLogValuesHelper
    {
        private static Type _formattedLogValues = Type.GetType("Microsoft.Extensions.Logging.FormattedLogValues, Microsoft.Extensions.Logging.Abstractions") ?? throw new Exception("Cannot find FormattedLogValues type");

        public static bool TryMapParameters<TState>(ref TState state, Func<string, object?, object?> mapper)
        {
            if (typeof(TState) != _formattedLogValues)
            {
                return false;
            }

            var values = state as IReadOnlyList<KeyValuePair< string, object?>>;

            if (values is null)
            {
                return false;
            }

            var list = new object?[values.Count - 1];
            bool anyChanged = false;

            for (var i = 0; i < values.Count - 1; i++)
            {
                var value = values[i];

                list[i] = mapper(value.Key, value.Value);

                if (list[i] != value.Value)
                {
                    anyChanged = true;
                }
            }

            if (!anyChanged)
                return false;

            var formatString = values[values.Count - 1].Value;
            
            if(Activator.CreateInstance(_formattedLogValues, formatString, list) is TState newState)
            {
                state = newState;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
