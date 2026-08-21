using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.CodeFixes;

internal static class ConfiguredCodeunitReplacementCodeFixHelper
{
    internal static MethodOrTriggerDeclarationSyntax? GetContainingMethodOrTrigger(Microsoft.Dynamics.Nav.CodeAnalysis.SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is MethodOrTriggerDeclarationSyntax methodOrTrigger)
                return methodOrTrigger;

            current = current.Parent;
        }

        return null;
    }

    internal static ApplicationObjectSyntax? GetContainingApplicationObject(Microsoft.Dynamics.Nav.CodeAnalysis.SyntaxNode node)
    {
        var current = node.Parent;
        while (current is not null)
        {
            if (current is ApplicationObjectSyntax applicationObject)
                return applicationObject;

            current = current.Parent;
        }

        return null;
    }

    internal static string? FindExistingCodeunitVariable(
        MethodOrTriggerDeclarationSyntax methodOrTrigger,
        ApplicationObjectSyntax containingObject,
        string targetCodeunitName)
    {
        var localVarName = FindCodeunitVariableInVarSection(methodOrTrigger.Variables, targetCodeunitName);
        if (localVarName is not null)
            return localVarName;

        return FindCodeunitVariableInMembers(containingObject.Members, targetCodeunitName);
    }

    internal static SyntaxNode AddLocalVariable(
        Microsoft.Dynamics.Nav.CodeAnalysis.SyntaxNode root,
        MethodOrTriggerDeclarationSyntax methodOrTrigger,
        string variableName,
        string codeunitName)
    {
        var variableDeclaration = CreateCodeunitVariableDeclaration(variableName, codeunitName);

        if (methodOrTrigger.Variables is VarSectionSyntax existingVarSection)
        {
            var newVariables = existingVarSection.Variables.Add(variableDeclaration);
            var newVarSection = existingVarSection.WithVariables(newVariables);
            return root.ReplaceNode(existingVarSection, newVarSection);
        }

        var newSection = SyntaxFactory.VarSection(
            SyntaxFactory.Token(EnumProvider.SyntaxKind.VarKeyword),
            new SyntaxList<VariableDeclarationBaseSyntax>().Add(variableDeclaration));

        var newMethodOrTrigger = methodOrTrigger switch
        {
            MethodDeclarationSyntax method => method.WithVariables(newSection),
            TriggerDeclarationSyntax trigger => trigger.WithVariables(newSection),
            _ => methodOrTrigger
        };

        return root.ReplaceNode(methodOrTrigger, newMethodOrTrigger);
    }

    private static string? FindCodeunitVariableInVarSection(VarSectionBaseSyntax? varSection, string targetCodeunitName)
    {
        if (varSection is null)
            return null;

        foreach (var variable in varSection.Variables)
        {
            if (IsCodeunitVariable(variable, targetCodeunitName))
                return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
        }

        return null;
    }

    private static string? FindCodeunitVariableInMembers(SyntaxList<MemberSyntax> members, string targetCodeunitName)
    {
        foreach (var member in members)
        {
            if (member is not GlobalVarSectionSyntax globalVarSection)
                continue;

            foreach (var variable in globalVarSection.Variables)
            {
                if (IsCodeunitVariable(variable, targetCodeunitName))
                    return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
            }
        }

        return null;
    }

    private static bool IsCodeunitVariable(VariableDeclarationBaseSyntax variable, string targetCodeunitName)
    {
        if (variable.Type is not TypeReferenceBaseSyntax typeReference)
            return false;

        if (typeReference.DataType.TypeName.Kind != EnumProvider.SyntaxKind.CodeunitKeyword)
            return false;

        return GetSubtypeName(typeReference.DataType).IsSameName(targetCodeunitName);
    }

    private static string? GetSubtypeName(DataTypeSyntax dataType)
    {
        if (dataType is not SubtypedDataTypeSyntax dataTypeWithSubtype)
            return null;

        if (dataTypeWithSubtype.Subtype is ObjectNameOrIdSyntax objectNameOrId &&
            objectNameOrId.Identifier is IdentifierNameSyntax identifierName)
        {
            return identifierName.Identifier.ValueText?.UnquoteIdentifier();
        }

        return dataTypeWithSubtype.Subtype.Identifier?.ToString().UnquoteIdentifier();
    }

    private static VariableDeclarationSyntax CreateCodeunitVariableDeclaration(string variableName, string codeunitName)
    {
        return SyntaxFactory.VariableDeclaration(
            default,
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(variableName)),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.ColonToken),
            CreateCodeunitTypeReference(codeunitName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));
    }

    private static SimpleTypeReferenceSyntax CreateCodeunitTypeReference(string codeunitName)
    {
        var codeunitObjectNameOrId = SyntaxFactory.ObjectNameOrId(
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(codeunitName)));

        var codeunitDataType = SyntaxFactory.SubtypedDataType(
            SyntaxFactory.ParseKeyword("Codeunit"),
            codeunitObjectNameOrId);

        return SyntaxFactory.SimpleTypeReference(codeunitDataType);
    }
}