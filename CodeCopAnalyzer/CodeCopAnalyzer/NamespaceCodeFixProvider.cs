using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CodeCopAnalyzer.Sample;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamespaceCodeFixProvider))]
public class NamespaceCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NamespaceAnalyzer.DiagnosticId);
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // var diagnosticNode = root?.FindNode(diagnosticSpan);
        var namespaceDeclaration = root
            ?.FindToken(diagnosticSpan.Start)
            .Parent
            ?.AncestorsAndSelf()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .First();

        if (namespaceDeclaration is null)
            return;
        
        // Get the project's expected namespace
        var projectName = context.Document.Project.Name;
        
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Set namespace to project name",
                createChangedSolution: c => RenameNamespace(context.Document, namespaceDeclaration, projectName, c),
                equivalenceKey: "namespace_fix"),
            diagnostic);
    }

    private async Task<Solution> RenameNamespace(Document document, BaseNamespaceDeclarationSyntax declaration, string projectName, CancellationToken cancellationToken)
    {
        // return await RenameNamespace2(document, declaration, projectName, cancellationToken);
        // Get the semantic model for the document
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Get the symbol representing the namespace from the syntax node
        if (semanticModel?.GetDeclaredSymbol(declaration, cancellationToken) is not INamespaceSymbol namespaceSymbol)
            return document.Project.Solution;

        // TODO: This is not working as expected
        // var partCount = declaration.Name.ToString().Split('.').Length - 1;
        // for (int i = 0; i < partCount && namespaceSymbol.ContainingNamespace is not null; i++)
        // {
        //     namespaceSymbol = namespaceSymbol.ContainingNamespace;
        // }
        
        // Create a new solution with the renamed namespace
        var newSolution = await Renamer.RenameSymbolAsync(
            document.Project.Solution,
            namespaceSymbol,
            new SymbolRenameOptions(),
            projectName,
            cancellationToken
        ).ConfigureAwait(false);

        return newSolution;
    }

    private async Task<Solution> RenameNamespace2(Document document, BaseNamespaceDeclarationSyntax declaration,
        string projectName, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        throw new System.NotImplementedException();
    }
}
