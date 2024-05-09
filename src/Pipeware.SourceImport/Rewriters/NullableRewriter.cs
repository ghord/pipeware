using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class NullableRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonValue? Enable { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public JsonValue? Disable { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            NullableRewriterCSharpVisitor rewriter;
            if (Enable is JsonValue value)
            {
                if (Disable is not null)
                {
                    context.Logger.LogError("Cannot set both #nullable enable and disable");

                    return tree;
                }

                if (value.TryGetValue(out bool enableFlag))
                {
                    rewriter = new NullableRewriterCSharpVisitor(enableFlag, false, context.Logger);
                }
                else if (value.GetValue<string>().Equals("warnings", StringComparison.OrdinalIgnoreCase))
                {
                    rewriter = new NullableRewriterCSharpVisitor(true, true, context.Logger);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (Disable is JsonValue disableValue)
            {
                if (disableValue.TryGetValue(out bool disableFlag))
                {
                    rewriter = new NullableRewriterCSharpVisitor(!disableFlag, false, context.Logger);
                }
                else if (disableValue.GetValue<string>().Equals("warnings", StringComparison.OrdinalIgnoreCase))
                {
                    rewriter = new NullableRewriterCSharpVisitor(false, true, context.Logger);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                context.Logger.LogError("No property for Nullable Rewriter set");

                return tree;
            }

            var root = rewriter.Visit(tree.GetRoot());

            if (!rewriter.Processed)
            {
                context.Logger.LogError("Could not set nullable directive");

                return tree;
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }


        class NullableRewriterCSharpVisitor : CSharpSyntaxRewriter
        {
            private NullableDirectiveTriviaSyntax _syntax;
            private ILogger _logger;

            public NullableRewriterCSharpVisitor(bool enable, bool warning, ILogger logger)
            {
                _logger = logger;

                if (warning)
                {
                    _syntax = SyntaxFactory.NullableDirectiveTrivia(
                        enable ? SyntaxFactory.Token(SyntaxKind.EnableKeyword) : SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                        SyntaxFactory.Token(SyntaxKind.WarningsKeyword),
                        true).NormalizeWhitespace();
                }
                else
                {
                    _syntax = SyntaxFactory.NullableDirectiveTrivia(
                          enable ? SyntaxFactory.Token(SyntaxKind.EnableKeyword) : SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                          true).NormalizeWhitespace();
                }
            }

            public bool Processed { get; set; }

            public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
            {
                if (!Processed)
                {
                    Processed = true;

                    _logger.LogDebug("Set nullable directive to [green]{directive}[/]", _syntax);



                    node = node.WithLeadingTrivia(node.GetLeadingTrivia().Add(SyntaxFactory.Trivia(_syntax)));
                }

                return base.VisitUsingDirective(node);
            }

            public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            {

                if (!Processed)
                {
                    Processed = true;

                    _logger.LogDebug("Set nullable directive to [green]{directive}[/]", _syntax);

                    node = node.WithLeadingTrivia(node.GetLeadingTrivia().Add(SyntaxFactory.Trivia(_syntax)));
                }

                return base.VisitFileScopedNamespaceDeclaration(node);
            }
            public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                if (!Processed)
                {
                    Processed = true;

                    _logger.LogDebug("Set nullable directive to [green]{directive}[/]", _syntax);

                    node = node.WithLeadingTrivia(node.GetLeadingTrivia().Add(SyntaxFactory.Trivia(_syntax)));
                }


                return base.VisitNamespaceDeclaration(node);
            }
        }
    }
}
