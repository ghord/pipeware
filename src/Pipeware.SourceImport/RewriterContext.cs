using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport
{
    public record class RewriterContext(ILogger Logger, string? Alias)
    {
     
    }
}
