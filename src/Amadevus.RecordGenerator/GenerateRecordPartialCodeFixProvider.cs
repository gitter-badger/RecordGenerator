﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amadevus.RecordGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenerateRecordPartialCodeFixProvider)), Shared]
    public sealed class GenerateRecordPartialCodeFixProvider : CodeFixProvider
    {
        private const string title = "Generate Record partial with ctor and mutators";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    RecordPartialMissingDiagnostic.DiagnosticId,
                    RecordPartialInvalidDiagnostic.DiagnosticId,
                    GeneratorVersionDifferentDiagnostic.DiagnosticId);
            }
        }
        
        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                // Find the type declaration identified by the diagnostic.
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

                // Register a code action that will invoke the fix.
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => FixMissingRecordPartialAsync(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private async Task<Solution> FixMissingRecordPartialAsync(Document document, TypeDeclarationSyntax declaration, CancellationToken c)
        {
            (document, declaration) = await AddPartialModifierIfRequired(document, declaration, c).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(c).ConfigureAwait(false);
            var typeSymbol = semanticModel.GetDeclaredSymbol(declaration);
            var generatedDocument = RecordPartialGenerator.GenerateRecordPartialDocument(document, declaration, typeSymbol, c);
            return generatedDocument.Project.Solution;
        }

        private async Task<(Document document, TypeDeclarationSyntax declaration)> AddPartialModifierIfRequired(Document document, TypeDeclarationSyntax declaration, CancellationToken c)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

            // add 'partial' to original declaration, if missing
            if (declaration.Modifiers.All(m => m.Kind() != SyntaxKind.PartialKeyword))
            {
                var annotation = new SyntaxAnnotation();
                var withPartialDeclaration = declaration.WithPartialModifier().WithAdditionalAnnotations(annotation);
                var root = await document.GetSyntaxRootAsync(c);
                var rootWithPartial = root.ReplaceNode(declaration, withPartialDeclaration);
                var newDocument = document.WithSyntaxRoot(rootWithPartial);
                var newRoot = await newDocument.GetSyntaxRootAsync(c).ConfigureAwait(false);
                var newDeclaration = newRoot.GetAnnotatedNodes(annotation).OfType<TypeDeclarationSyntax>().First();
                return (newDocument, newDeclaration);
            }
            return (document, declaration);
        }
    }
}
