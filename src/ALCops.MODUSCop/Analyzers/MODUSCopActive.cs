using System.Collections.Immutable;
using ALCops.Common.Extensions;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Packaging;

namespace ALCops.MODUSCop.Analyzers;

[DiagnosticAnalyzer]
public sealed class MODUSCopActive : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.MODUSCopActive);

    public override void Initialize(AnalysisContext context) =>
        context.RegisterCompilationAction(AnalyzeManifest);

    private static void AnalyzeManifest(CompilationAnalysisContext ctx)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        NavAppManifest? manifest = ManifestHelper.GetManifest(ctx.Compilation);
        if (manifest is null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MODUSCopActive,
            manifest.GetDiagnosticLocation("id")));
    }
}
