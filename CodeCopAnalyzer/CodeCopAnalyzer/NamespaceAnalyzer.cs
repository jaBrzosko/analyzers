using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCopAnalyzer.Sample;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NamespaceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UXE0002";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Namespace does not match project name",
        "Namespace '{0}' does not match expected project namespace '{1}'",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNamespace,
            SyntaxKind.NamespaceDeclaration,
            SyntaxKind.FileScopedNamespaceDeclaration);
    }

    private void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
            return;
        
        // Get the project's expected namespace
        var projectName = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.rootnamespace", out var expectedNamespace) ? expectedNamespace : null;

        if (projectName == null)
        {
            return; // Cannot determine the expected namespace
        }

        var actualNamespace = namespaceDeclaration.Name.ToString();

        if (actualNamespace != projectName)
        {
            var diagnostic = Diagnostic.Create(Rule, namespaceDeclaration.Name.GetLocation(), actualNamespace, expectedNamespace);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
