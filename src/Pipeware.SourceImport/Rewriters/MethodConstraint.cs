using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport.Rewriters
{
    public class MethodConstraint
    {
        public required HashSet<string> Methods { get; init; }
        public int? ParameterCount { get; init; }
        public string? Type { get; init; }
        public int[]? Arities { get; init; }
        public ParameterConstraint[]? Parameters { get; init; }

        public bool MatchesMethodName(SyntaxToken identifier)
        {
            return Methods.Contains(identifier.ToString());
        }

        public bool MatchesArity(TypeParameterListSyntax? typeParameters)
        {
            if (Arities == null)
                return true;

            return Arities.Contains(typeParameters?.Parameters.Count ?? 0);
        }

        public bool MatchesDeclaringType(SyntaxNode node)
        {
            if (Type == null)
                return true;

            return node.FirstAncestorOrSelf<TypeDeclarationSyntax>()?.Identifier.ToString().Equals(Type) ?? false;
        }

        public bool MatchesParameters(ParameterListSyntax parameterList)
        {
            if (ParameterCount != null && parameterList.Parameters.Count != ParameterCount)
                return false;

            if (Parameters != null)
            {
                var unusedConstraints = Parameters.Select(p => p.Parameter).ToHashSet();
                var constraints = Parameters.ToDictionary(c => c.Parameter, c => c);

                foreach (var parameter in parameterList.Parameters)
                {
                    if (unusedConstraints.Remove(parameter.Identifier.ToString()))
                    {
                        var constraint = constraints[parameter.Identifier.ToString()];

                        if (constraint.Type != null || constraint.Types != null)
                        {
                            if (parameter.Type is null)
                                return false;

                            bool anyMatched = false;

                            foreach (var type in constraint.Types ?? [constraint.Type!])
                            {
                                if (parameter.Type is IdentifierNameSyntax identifierName)
                                {
                                    if (identifierName.Identifier.ToString().Equals(type))
                                        anyMatched = true;
                                }
                                else if (parameter.Type is GenericNameSyntax genericName)
                                {
                                    if (genericName.Identifier.ToString().Equals(type))
                                        anyMatched = true;
                                }
                                else if (parameter.Type.ToString().Equals(type))
                                {
                                    anyMatched = true;
                                }
                            }

                            if (!anyMatched)
                                return false;
                        }
                    }
                }

                if (unusedConstraints.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
