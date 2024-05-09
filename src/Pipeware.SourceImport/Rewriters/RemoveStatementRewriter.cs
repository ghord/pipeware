using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class RemoveStatementRewriter : IImportRewriter
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? If { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Ifs { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Expression { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Expressions { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? VariableDeclaration { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? VariableDeclarations { get; set; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree)
        {
            if ((If ?? VariableDeclaration ?? Expression) == null && (Expressions ?? Ifs ?? VariableDeclarations) == null)
            {
                context.Logger.LogError("Remove statement rewriter has neither property set");

                return tree;
            }

            var root = tree.GetRoot();

            foreach(var ifStatement in Ifs ?? [ If! ])
            {
                if (ifStatement is null)
                    continue;

                var rewriter = new RemoveIfCSharpRewriter(ifStatement, null, null, context.Logger);

                root = rewriter.Visit(root);

                if(!rewriter.Removed)
                {
                    context.Logger.LogWarning("Failed to remove if statement '{If}'", Markup.Escape(ifStatement));
                }
            }

            foreach(var expression in Expressions ?? [Expression!])
            {
                if (expression is null)
                    continue;

                var rewriter = new RemoveIfCSharpRewriter(null, expression, null, context.Logger);

                root = rewriter.Visit(root);

                if(!rewriter.Removed)
                {
                    context.Logger.LogWarning("Failed to remove expression statement '{Expression}'", Markup.Escape(expression));
                }
            }

            foreach (var variableDeclaration in VariableDeclarations ?? [VariableDeclaration!])
            {
                if (variableDeclaration is null)
                    continue;   

                var rewriter = new RemoveIfCSharpRewriter(null, null, variableDeclaration, context.Logger);

                root = rewriter.Visit(root);

                if (!rewriter.Removed)
                {
                    context.Logger.LogWarning("Failed to remove variable declaration statement '{VariableDeclaration}'", Markup.Escape(variableDeclaration));
                }
            }

            return tree.WithRootAndOptions(root, tree.Options);
        }

        class RemoveIfCSharpRewriter : CSharpSyntaxRewriter
        {
            private string? _ifExpression;
            private string? _expression;
            private ILogger _logger;
            private string? _variableDeclaration;

            public bool Removed { get; private set; }

            public RemoveIfCSharpRewriter(string? ifExpression, string? expression, string? variableDeclaration, ILogger logger)
            {
                _ifExpression = ifExpression != null ? SyntaxFactory.ParseExpression(ifExpression).NormalizeWhitespace().ToString() : null;
                _expression = expression != null ? SyntaxFactory.ParseExpression(expression).NormalizeWhitespace().ToString() : null;
                _logger = logger;
                _variableDeclaration = variableDeclaration;
            }

            public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
            {
                if (_ifExpression != null && node.Condition.NormalizeWhitespace().ToString().Equals(_ifExpression))
                {
                    _logger.LogDebug("Removed if statement with condition [teal]{condition}[/]", node.Condition);

                    Removed = true;

                    if (node.Else is not null)
                    {
                        return Visit(node.Else.Statement);
                    }
                    else
                    {
                        return null;
                    }
                }

                return base.VisitIfStatement(node);
            }

            public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                if (_expression != null && node.Expression.NormalizeWhitespace().ToString().Equals(_expression))
                {
                    _logger.LogDebug("Removed expression statement [teal]{expr}[/]", node.Expression);

                    Removed = true;

                    return null;
                }

                return base.VisitExpressionStatement(node);
            }

            public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
            {
                if (_variableDeclaration != null && node.Declaration.Variables.Count == 1 && node.Declaration.Variables[0].Identifier.ToString().Equals(_variableDeclaration))
                {
                    _logger.LogDebug("Removed variable declaration statement [teal]{variable}[/]", node);

                    Removed = true;

                    return null;
                }

                return base.VisitLocalDeclarationStatement(node);
            }

        }
    }
}
