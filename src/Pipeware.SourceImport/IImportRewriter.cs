using Microsoft.CodeAnalysis;
using Pipeware.SourceImport.Rewriters;
using System.Text.Json.Serialization;

namespace Pipeware.SourceImport
{
    [JsonDerivedType(typeof(NamespaceDeclarationRewriter), "namespaceDeclaration")]
    [JsonDerivedType(typeof(RenameTypeRewriter), "renameType")]
    [JsonDerivedType(typeof(RenamePropertyRewriter), "renameProperty")]
    [JsonDerivedType(typeof(RenameMethodRewriter), "renameMethod")]
    [JsonDerivedType(typeof(RenameFileRewriter), "renameFile")]
    [JsonDerivedType(typeof(RemoveTypeRewriter), "removeType")]
    [JsonDerivedType(typeof(RemoveUsingRewriter), "removeUsing")]
    [JsonDerivedType(typeof(RemoveStatementRewriter), "removeStatement")]
    [JsonDerivedType(typeof(QualifyTypeRewriter), "qualifyType")]
    [JsonDerivedType(typeof(AddUsingRewriter), "addUsing")]
    [JsonDerivedType(typeof(RemovePropertyRewriter), "removeProperty")]
    [JsonDerivedType(typeof(RemoveAttributeRewriter), "removeAttribute")]
    [JsonDerivedType(typeof(PropertyToMethodRewriter), "propertyToMethod")]
    [JsonDerivedType(typeof(RemoveMethodRewriter), "removeMethod")]
    [JsonDerivedType(typeof(RemoveFieldRewriter), "removeField")]
    [JsonDerivedType(typeof(RemoveConstructorRewriter), "removeCtor")]
    [JsonDerivedType(typeof(MakeMethodGenericRewriter), "makeMethodGeneric")]
    [JsonDerivedType(typeof(MakeTypeGenericRewriter), "makeTypeGeneric")]
    [JsonDerivedType(typeof(ReplaceCommentRewriter), "replaceComment")]
    [JsonDerivedType(typeof(ReplaceLiteralRewriter), "replaceLiteral")]
    [JsonDerivedType(typeof(ReplaceExpressionRewriter), "replaceExpression")]
    [JsonDerivedType(typeof(ReplaceParameterRewriter), "replaceParameter")]
    [JsonDerivedType(typeof(NullableRewriter), "nullable")]
    [JsonDerivedType(typeof(MakePartialRewriter), "makePartial")]
    [JsonDerivedType(typeof(RemoveParameterRewriter), "removeParameter")]
    [JsonDerivedType(typeof(AddInterfaceRewriter), "addInterface")]
    [JsonDerivedType(typeof(RemoveExpressionRewriter), "removeExpression")]
    [JsonDerivedType(typeof(AddAttributeRewriter), "addAttribute")]
    [JsonDerivedType(typeof(MakeNullableRewriter), "makeNullable")]
    [JsonDerivedType(typeof(RenameParameterRewriter), "renameParameter")]
    [JsonDerivedType(typeof(RemoveGenericParameterRewriter), "removeGenericParameter")]
    [JsonDerivedType(typeof(RemoveInterfaceRewriter), "removeInterface")]
    [JsonDerivedType(typeof(ReplaceStatementRewriter), "replaceStatement")]
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    public interface IImportRewriter
    {
        SyntaxTree Rewrite(RewriterContext context, SyntaxTree tree);
    }
}