using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCopAnalyzer.Sample;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EnumNameAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UXE0001";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Enum name must end with 'Enum'",
        "Enum '{0}' does not end with 'Enum'",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
    
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext obj)
    {
        if (obj.Symbol is not INamedTypeSymbol namedType)
            return;

        if (namedType.TypeKind != TypeKind.Enum)
            return;

        if (!namedType.Name.EndsWith("Enum"))
        {
            var diagnostic = Diagnostic.Create(Rule, namedType.Locations[0], namedType.Name);
            obj.ReportDiagnostic(diagnostic);
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
}
