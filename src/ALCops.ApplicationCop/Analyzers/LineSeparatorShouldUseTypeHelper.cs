using System.Collections.Immutable;
using ALCops.Common.Extensions;
using ALCops.Common.Reflection;
using ALCops.Common.Settings;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Semantics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;

namespace ALCops.ApplicationCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class LineSeparatorShouldUseTypeHelper : DiagnosticAnalyzer
{
    private const int CrlfCarriageReturnAscii = 13;
    private const int LfAscii = 10;
    private const string DefaultVariableDeclaration = "TypeHelper: Codeunit \"Type Helper\";";
    private const string LfSeparatorMethodKey = "LFSeparator";
    private const string CrlfSeparatorMethodKey = "CRLFSeparator";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.LineSeparatorShouldUseTypeHelper
        );

    public override void Initialize(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeAssignmentStatement,
            EnumProvider.OperationKind.AssignmentStatement);

    private static void AnalyzeAssignmentStatement(OperationAnalysisContext ctx)
    {
        if (ctx.IsObsolete())
            return;

        if (ctx.Operation is not IAssignmentStatement assignment)
            return;

        if (!TryGetIntLiteralValue(assignment.Value, out var rhsValue))
            return;

        if (rhsValue == CrlfCarriageReturnAscii && TryGetCrlfPair(ctx, assignment, out _))
        {
            ReportDiagnostic(ctx);
            return;
        }

        if (rhsValue != LfAscii ||
            IsCrlfSecondAssignment(ctx, assignment) ||
            !IsValidLfSeparatorTarget(assignment.Target))
        {
            return;
        }

        ReportDiagnostic(ctx);
    }

    private static void ReportDiagnostic(OperationAnalysisContext ctx)
    {
        var properties = CreateReplacementProperties(ctx);

        ctx.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.LineSeparatorShouldUseTypeHelper,
                ctx.Operation.Syntax.GetLocation(),
                properties,
                Array.Empty<object>()));
    }

    private static bool IsCrlfSecondAssignment(OperationAnalysisContext ctx, IAssignmentStatement assignment)
    {
        var semanticModel = ctx.Compilation.GetSemanticModel(ctx.Operation.Syntax.SyntaxTree);
        if (!TryGetAdjacentAssignment(semanticModel, assignment, ctx.CancellationToken, -1, out var previousAssignment))
            return false;

        return TryGetIntLiteralValue(previousAssignment.Value, out var previousValue) &&
               previousValue == CrlfCarriageReturnAscii &&
               IsCrlfPair(semanticModel, previousAssignment, assignment, ctx.CancellationToken);
    }

    private static bool TryGetCrlfPair(
        OperationAnalysisContext ctx,
        IAssignmentStatement assignment,
        out IAssignmentStatement nextAssignment)
    {
        nextAssignment = null!;

        var semanticModel = ctx.Compilation.GetSemanticModel(ctx.Operation.Syntax.SyntaxTree);
        if (!TryGetAdjacentAssignment(semanticModel, assignment, ctx.CancellationToken, 1, out nextAssignment))
            return false;

        return TryGetIntLiteralValue(nextAssignment.Value, out var nextValue) &&
               nextValue == LfAscii &&
               IsCrlfPair(semanticModel, assignment, nextAssignment, ctx.CancellationToken);
    }

    private static bool IsCrlfPair(
        SemanticModel semanticModel,
        IAssignmentStatement carriageReturnAssignment,
        IAssignmentStatement lineFeedAssignment,
        CancellationToken cancellationToken)
    {
        return IsTextCrlfPair(
                   semanticModel,
                   carriageReturnAssignment.Target,
                   lineFeedAssignment.Target,
                   cancellationToken) ||
               (IsCharVariable(carriageReturnAssignment.Target) && IsCharVariable(lineFeedAssignment.Target));
    }

    private static bool IsTextCrlfPair(
        SemanticModel semanticModel,
        IOperation carriageReturnTarget,
        IOperation lineFeedTarget,
        CancellationToken cancellationToken)
    {
        return TryGetTextElementAccess(
                   semanticModel,
                   carriageReturnTarget,
                   1,
                   cancellationToken,
                   out var carriageReturnVariableName) &&
               TryGetTextElementAccess(
                   semanticModel,
                   lineFeedTarget,
                   2,
                   cancellationToken,
                   out var lineFeedVariableName) &&
               carriageReturnVariableName.IsSameName(lineFeedVariableName);
    }

    private static bool TryGetTextElementAccess(
        SemanticModel semanticModel,
        IOperation targetOperation,
        int expectedIndex,
        CancellationToken cancellationToken,
        out string? variableName)
    {
        variableName = null;

        if (targetOperation.Kind != EnumProvider.OperationKind.FieldAccess ||
            targetOperation.Syntax is not ElementAccessExpressionSyntax elementAccess ||
            elementAccess.Expression is not IdentifierNameSyntax identifierName)
        {
            return false;
        }

        if (!TryGetElementAccessIndex(elementAccess, out var index) || index != expectedIndex)
            return false;

        var variableOperation = semanticModel.GetOperation(elementAccess.Expression, cancellationToken);
        if (variableOperation?.Type?.GetNavTypeKindSafe() != EnumProvider.NavTypeKind.Text)
            return false;

        variableName = identifierName.Identifier.ValueText;
        return !string.IsNullOrWhiteSpace(variableName);
    }

    private static bool TryGetAdjacentAssignment(
        SemanticModel semanticModel,
        IAssignmentStatement assignment,
        CancellationToken cancellationToken,
        int offset,
        out IAssignmentStatement adjacentAssignment)
    {
        adjacentAssignment = null!;

        if (assignment.Syntax is not AssignmentStatementSyntax assignmentSyntax ||
            assignmentSyntax.Parent is not BlockSyntax block)
        {
            return false;
        }

        for (int index = 0; index < block.Statements.Count; index++)
        {
            if (!ReferenceEquals(block.Statements[index], assignmentSyntax))
                continue;

            var adjacentIndex = index + offset;
            if (adjacentIndex < 0 || adjacentIndex >= block.Statements.Count ||
                block.Statements[adjacentIndex] is not AssignmentStatementSyntax adjacentSyntax)
            {
                return false;
            }

            var operation = semanticModel.GetOperation(adjacentSyntax, cancellationToken) as IAssignmentStatement;
            if (operation is null)
                return false;

            adjacentAssignment = operation;
            return true;
        }

        return false;
    }

    private static ImmutableDictionary<string, string> CreateReplacementProperties(OperationAnalysisContext ctx)
    {
        var settings = ALCopsSettingsProvider.GetSettings(ctx.Compilation.FileSystem);
        var replacement = CodeFixReplacementResolver.ResolveCodeFixReplacement(
            settings,
            DiagnosticIds.LineSeparatorShouldUseTypeHelper,
            new CodeFixReplacementDefaults(
                DefaultVariableDeclaration,
                new Dictionary<string, string>
                {
                    [LfSeparatorMethodKey] = LfSeparatorMethodKey,
                    [CrlfSeparatorMethodKey] = CrlfSeparatorMethodKey,
                }),
            NamingPatternTarget.LocalVariable,
            ConfiguredCodeFixReplacementAnalyzerHelper.CollectReservedNames(ctx.ContainingSymbol));

        return CodeFixReplacementPropertyBag.Create(replacement);
    }

    private static bool IsValidLfSeparatorTarget(IOperation targetOperation)
    {
        // Code[1], Code[2], Text[1], Text[2]
        if (targetOperation.Kind == EnumProvider.OperationKind.FieldAccess &&
            IsValidTextOrCodeArrayAccess(targetOperation.Syntax))
        {
            return true;
        }

        // Char variable
        if (targetOperation.Kind == EnumProvider.OperationKind.LocalReferenceExpression ||
            targetOperation.Kind == EnumProvider.OperationKind.GlobalReferenceExpression)
        {
            return IsCharVariable(targetOperation);
        }

        return false;
    }

    private static bool IsValidTextOrCodeArrayAccess(SyntaxNode? targetSyntax)
    {
        if (targetSyntax is not ElementAccessExpressionSyntax elementAccess)
            return false;

        return TryGetElementAccessIndex(elementAccess, out var indexValue) &&
               (indexValue == 1 || indexValue == 2);
    }

    private static bool TryGetElementAccessIndex(ElementAccessExpressionSyntax elementAccess, out int index)
    {
        index = default;

        var argumentList = elementAccess.ArgumentList;
        if (argumentList is null || argumentList.Arguments.Count != 1 ||
            argumentList.Arguments[0] is not LiteralExpressionSyntax indexLiteral ||
            indexLiteral.Literal is not Int32SignedLiteralValueSyntax indexInt)
        {
            return false;
        }

        return int.TryParse(indexInt.GetIdentifierOrLiteralValue(), out index);
    }

    private static bool IsCharVariable(IOperation targetOperation)
    {
        if (targetOperation.GetSymbol() is not IVariableSymbol variableSymbol)
            return false;

        return variableSymbol
            .GetTypeSymbol()
            .GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Char;
    }

    private static bool TryGetIntLiteralValue(IOperation operation, out int value)
    {
        value = default;

        return operation.Syntax is LiteralExpressionSyntax literalExpr &&
               TryGetIntFromLiteralExpression(literalExpr, out value);
    }

    private static bool TryGetIntFromLiteralExpression(LiteralExpressionSyntax literalExpr, out int value)
    {
        value = default;

        if (literalExpr.Literal is not Int32SignedLiteralValueSyntax intLiteral)
            return false;

        return int.TryParse(intLiteral.GetIdentifierOrLiteralValue(), out value);
    }
}