using System.Collections.Immutable;
using ALCops.Common.Reflection;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Symbols;

namespace ALCops.Common;

/// <summary>
/// Single source of truth for built-in AL methods that unconditionally end execution of the
/// calling code (<c>Error</c>, <c>FieldError</c>). Consumed by PC0038, LC0089/LC0090, and FC0007
/// through the single <see cref="IsFlowTerminatingCall(IOperation?)"/> rule, so their terminator
/// sets and their matching semantics cannot drift apart.
/// </summary>
public static class FlowTerminatingBuiltIns
{
    // Names of built-in methods that never return control to the caller.
    private static readonly ImmutableHashSet<string> MethodNames =
        ImmutableHashSet.Create(SemanticFacts.NameEqualityComparer, "Error", "FieldError");

    /// <summary>
    /// Returns true when <paramref name="operation"/> is a call to a built-in AL method that never
    /// returns control to the caller (<c>Error</c>, <c>FieldError</c>).
    /// Accepts either a clean bind to the built-in (<see cref="MethodKind.BuiltInMethod"/>) or an invalid
    /// call whose synthesized target still names the built-in and whose containing type is Dialog, Record or
    /// FieldRef. The latter is what <c>Binder.CreateBadCall</c> produces while arguments do not bind
    /// (undefined variable, wrong arity, mid-edit) — without it PC0038/FC0007/LC0089 flicker while typing.
    /// The containing type of that synthesized symbol is the receiver type (<c>Rec.FieldError(...)</c>,
    /// <c>FldRef.FieldError</c>) or, for the receiver-less <c>Error(...)</c>, the static <c>Dialog</c> class
    /// that hosts the built-in <c>Error</c>/<c>Message</c>/<c>Confirm</c> methods.
    /// User-defined procedures with the same name never match: a positive bind has MethodKind.Method,
    /// an invalid call on a user or referenced-app object has a Codeunit/Page/... containing type, and the
    /// compiler rejects members that shadow a built-in on Record/FieldRef (AL0754/AL0755
    /// "already defines a built-in member"). DeclaringSyntaxReference cannot be used to detect
    /// built-ins: procedures from referenced apps have none either.
    /// </summary>
    public static bool IsFlowTerminatingCall(IOperation? operation) =>
        operation is IInvocationExpression { TargetMethod: IMethodSymbol method } &&
        MethodNames.Contains(method.Name) &&
        (method.MethodKind == EnumProvider.MethodKind.BuiltInMethod ||
         (operation.IsInvalid && IsBuiltInReceiver(method.ContainingSymbol)));

    private static bool IsBuiltInReceiver(ISymbol? containingSymbol) =>
        containingSymbol is ITypeSymbol type &&
        (type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Dialog ||
         type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.Record ||
         type.GetNavTypeKindSafe() == EnumProvider.NavTypeKind.FieldRef);
}
