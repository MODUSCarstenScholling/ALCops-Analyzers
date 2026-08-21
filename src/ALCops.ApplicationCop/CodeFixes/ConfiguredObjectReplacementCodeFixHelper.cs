using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using Microsoft.Dynamics.Nav.CodeAnalysis.Utilities;

namespace ALCops.ApplicationCop.CodeFixes;

internal sealed class ObjectReplacementTarget
{
    internal string VariableName { get; }
    internal bool RequiresLocalDeclaration { get; }

    internal ObjectReplacementTarget(string variableName, bool requiresLocalDeclaration)
    {
        VariableName = variableName;
        RequiresLocalDeclaration = requiresLocalDeclaration;
    }
}

internal static class ConfiguredObjectReplacementCodeFixHelper
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

    internal static ObjectReplacementTarget? ResolveReplacementTarget(
        MethodOrTriggerDeclarationSyntax methodOrTrigger,
        ApplicationObjectSyntax containingObject,
        CodeFixReplacementResolution replacement)
    {
        var existingVariableName = FindExistingObjectVariable(
            methodOrTrigger,
            containingObject,
            replacement.VariableTypeKeyword,
            replacement.VariableSubtypeName);

        var variableName = existingVariableName ?? replacement.VariableName;
        if (string.IsNullOrWhiteSpace(variableName))
            return null;

        return new ObjectReplacementTarget(variableName, existingVariableName is null);
    }

    internal static SyntaxNode AddLocalVariable(
        Microsoft.Dynamics.Nav.CodeAnalysis.SyntaxNode root,
        MethodOrTriggerDeclarationSyntax methodOrTrigger,
        string variableName,
        CodeFixReplacementResolution replacement)
    {
        var variableDeclaration = CreateObjectVariableDeclaration(
            variableName,
            replacement.VariableTypeKeyword,
            replacement.VariableSubtypeName);

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

    private static string? FindExistingObjectVariable(
        MethodOrTriggerDeclarationSyntax methodOrTrigger,
        ApplicationObjectSyntax containingObject,
        string objectTypeKeyword,
        string objectName)
    {
        var localVarName = FindObjectVariableInVarSection(
            methodOrTrigger.Variables,
            objectTypeKeyword,
            objectName);
        if (localVarName is not null)
            return localVarName;

        return FindObjectVariableInMembers(
            containingObject.Members,
            objectTypeKeyword,
            objectName);
    }

    private static string? FindObjectVariableInVarSection(
        VarSectionBaseSyntax? varSection,
        string objectTypeKeyword,
        string objectName)
    {
        if (varSection is null)
            return null;

        foreach (var variable in varSection.Variables)
        {
            if (IsObjectVariable(variable, objectTypeKeyword, objectName))
                return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
        }

        return null;
    }

    private static string? FindObjectVariableInMembers(
        SyntaxList<MemberSyntax> members,
        string objectTypeKeyword,
        string objectName)
    {
        foreach (var member in members)
        {
            if (member is not GlobalVarSectionSyntax globalVarSection)
                continue;

            foreach (var variable in globalVarSection.Variables)
            {
                if (IsObjectVariable(variable, objectTypeKeyword, objectName))
                    return variable.GetIdentifierNameSyntax().Identifier.ValueText?.UnquoteIdentifier();
            }
        }

        return null;
    }

    private static bool IsObjectVariable(
        VariableDeclarationBaseSyntax variable,
        string objectTypeKeyword,
        string objectName)
    {
        if (variable.Type is not TypeReferenceBaseSyntax typeReference)
            return false;

        if (!typeReference.DataType.TypeName.ToString().IsSameName(objectTypeKeyword))
            return false;

        return GetSubtypeName(typeReference.DataType).IsSameName(objectName);
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

    private static VariableDeclarationSyntax CreateObjectVariableDeclaration(
        string variableName,
        string objectTypeKeyword,
        string objectName)
    {
        return SyntaxFactory.VariableDeclaration(
            default,
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(variableName)),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.ColonToken),
            CreateObjectTypeReference(objectTypeKeyword, objectName),
            SyntaxFactory.Token(EnumProvider.SyntaxKind.SemicolonToken));
    }

    private static SimpleTypeReferenceSyntax CreateObjectTypeReference(string objectTypeKeyword, string objectName)
    {
        var objectNameOrId = SyntaxFactory.ObjectNameOrId(
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(objectName)));

        var objectDataType = SyntaxFactory.SubtypedDataType(
            SyntaxFactory.ParseKeyword(objectTypeKeyword),
            objectNameOrId);

        return SyntaxFactory.SimpleTypeReference(objectDataType);
    }
}