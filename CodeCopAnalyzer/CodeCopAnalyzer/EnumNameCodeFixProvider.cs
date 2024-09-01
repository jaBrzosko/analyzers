using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace CodeCopAnalyzer.Sample;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EnumNameCodeFixProvider))]
public class EnumNameCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(EnumNameAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var diagnosticNode = root?.FindNode(diagnosticSpan);

        if (diagnosticNode is not EnumDeclarationSyntax declaration)
            return;
        
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Append 'Enum' to the enum name",
                createChangedSolution: c => AppendEnumKeyword(context.Document, declaration, c),
                equivalenceKey: "temp_fix_name"),
            diagnostic);
    }

    private async Task<Solution> AppendEnumKeyword(Document document, EnumDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var newName = declaration.Identifier.Text + "Enum";
        
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        
        var typeSymbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
        if (typeSymbol == null)
            return document.Project.Solution;
        
        var newSolution = await Renamer
            .RenameSymbolAsync(document.Project.Solution, typeSymbol, new SymbolRenameOptions(), newName, cancellationToken)
            .ConfigureAwait(false);
        
        return newSolution;
    }
}
