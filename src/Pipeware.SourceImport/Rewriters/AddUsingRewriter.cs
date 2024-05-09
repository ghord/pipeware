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
    public class AddUsingRewriter : IImportRewriter
    {
        public required string Using { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Static { get; init; }

        public SyntaxTree Rewrite(RewriterContext context, SyntaxTree syntaxTree)
        {
            // Parse the syntax tree root
            var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

            if (root == null)
            {
                context.Logger.LogError("Cannot add using, invalid syntax");
                return syntaxTree;
            }

            // Check if the using directive already exists
            var existingUsing = root.Usings
                .Any(u => u.Name is not null && u.Name.ToString().Equals(Using));

            if (existingUsing)
            {
                // The using directive already exists, no changes needed
                return syntaxTree;
            }

            // Create a new using directive
            var newUsingDirective = Static ?
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseTypeName(Using)) :
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Using));

            newUsingDirective = newUsingDirective.NormalizeWhitespace();

            if (root.Usings.Any())
            {
                // Ensure proper formatting
                newUsingDirective = newUsingDirective.WithTriviaFrom(root.Usings.Last().WithoutLeadingTrivia());
            }
            else
            {
                newUsingDirective = newUsingDirective.WithLeadingTrivia(root.GetLeadingTrivia()).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                root = root.WithoutLeadingTrivia();
            }

            // Add the new using directive to the syntax tree
            var newRoot = root.AddUsings(newUsingDirective);

            context.Logger.LogDebug("Added using [green]{Using}[/]", Using);

            // Return the new syntax tree
            return syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);
        }


    }
}
