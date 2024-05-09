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
    public class RenameMethodRewriter : IImportRewriter
    {
        public required string SourceName { get; set; }
        public required string TargetName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]

        public bool Multiple { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            var rewriter = new RenameMethodCSharpRewriter(SourceName, TargetName, Multiple, context.Logger);

            var root = rewriter.Visit(tree.GetRoot());

            if(!rewriter.Renamed)
            {
                context.Logger.LogWarning($"Method {SourceName} was not renamed");
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RenameMethodCSharpRewriter : CSharpSyntaxRewriter
        {
            private string _source;
            private string _target;
            private ILogger _logger;
            private bool _multiple;

            public bool Renamed { get; private set; }

            public RenameMethodCSharpRewriter(string source, string target, bool multiple, ILogger logger)
            {
                _source = source;
                _target = target;
                _logger = logger;
                _multiple = multiple;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.ToString().Equals(_source))
                {
                    if (!_multiple && Renamed)
                    {
                        _logger.LogWarning("Method {method} renamed more than once", node.Identifier);
                    }

                    _logger.LogDebug("Renamed method [teal]{methodName}{params)}[/] to [green]{target}[/]", node.Identifier, node.ParameterList, _target);

                    Renamed = true;

                    return Visit(node.WithIdentifier(SyntaxFactory.Identifier(_target).WithTriviaFrom(node.Identifier)));
                }


                return base.VisitMethodDeclaration(node);
            }
        }
    }
}
