using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class RemoveInterfaceRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Interface { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Interfaces { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if (Interfaces is null && Interface is null)
            {
                context.Logger.LogWarning("No interfaces were provided to the remove interface rewriter.");

                return tree;
            }

            var root = tree.GetRoot();

            foreach (var iface in Interfaces ?? [Interface!])
            {
                var rewriter = new RemoveInterfaceCSharpRewriter(iface, context.Logger);

                root = rewriter.Visit(root);

                if (!rewriter.Removed && !Optional)
                {
                    context.Logger.LogWarning("No interface {@Interface} was found to remove.", iface);
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemoveInterfaceCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _interface;
            private ILogger _logger;

            public RemoveInterfaceCSharpRewriter(string @interface, ILogger logger)
            {
                _interface = @interface;
                _logger = logger;
            }

            public bool Removed { get; private set; }

            public override SyntaxNode? VisitBaseList(BaseListSyntax node)
            {
                if (node.Types.FirstOrDefault(t => t.Type is IdentifierNameSyntax id && id.Identifier.ToString().Equals(_interface)) is { } typeToRemove)
                {
                    _logger.LogDebug("Removed interface [teal]{Interface}[/] from type.", _interface);

                    Removed = true;

                    if (node.Types.Count == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return Visit(node.WithTypes(node.Types.Remove(typeToRemove)));
                    }
                }

                return base.VisitBaseList(node);
            }
        }
    }
}
