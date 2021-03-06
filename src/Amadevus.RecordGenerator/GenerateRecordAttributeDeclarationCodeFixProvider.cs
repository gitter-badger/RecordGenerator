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
using Microsoft.CodeAnalysis.Formatting;

namespace Amadevus.RecordGenerator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenerateRecordAttributeDeclarationCodeFixProvider)), Shared]
    public sealed class GenerateRecordAttributeDeclarationCodeFixProvider : CodeFixProvider
    {
        private const string title = "Generate RecordAttribute declaration";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(RecordAttributeDeclarationMissingDiagnostic.DiagnosticId);
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
                // Find the attribute identifier syntax
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IdentifierNameSyntax>().First();
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => CreateRecordAttributeDeclarationDocument(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }
        
        private Task<Solution> CreateRecordAttributeDeclarationDocument(Document document, IdentifierNameSyntax declaration, CancellationToken cancellationToken)
        {
            // get the namespace of document or namespace from attribute usage

            var namespaces = declaration
                .Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .Reverse()
                .Select(nsSyntax => nsSyntax.Name.ToString())
                .ToArray();
            var targetNamespace = string.Join(".", namespaces);
            var text = RecordAttributeDeclarationSource(targetNamespace);

            var tree = CSharpSyntaxTree.ParseText(text, cancellationToken: cancellationToken);
            var formattedRoot = Formatter.Format(tree.GetRoot(), document.Project.Solution.Workspace, cancellationToken: cancellationToken);
            var doc = document.Project.AddDocument(RecordAttributeProperties.Filename, formattedRoot, document.Folders);
            return Task.FromResult(doc.Project.Solution);
        }

        internal static string RecordAttributeDeclarationSource(string targetNamespace)
        {
            var text =
$@"namespace {targetNamespace}
{{
    /// <summary>
    /// Identifies class or struct that is supposed to have a partial with ctor and mutators generated by source generator.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode(""{nameof(RecordGenerator)}"", ""{Properties.VersionString}"")]
    [System.Diagnostics.Conditional(""NEVER"")]
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal sealed class RecordAttribute : System.Attribute
    {{
        public RecordAttribute()
        {{
        }}


        public string PrimaryCtorAccess {{ get; set; }} = ""public"";

        /// <summary>
        /// Gets or sets whether mutator methods should be generated (e.g. WithSurname). Default is true.
        /// </summary>
        public bool GenerateMutators {{ get; set; }} = true;
    }}
}}
";

            /* to add sometime in future:

        /// <summary>
        /// Gets or sets whether collection mutator methods should be generated (e.g. AddItems, ReplaceItems). Default is true.
        /// </summary>
        public bool GenerateCollectionMutators {{ get; set; }} = true;
        
             */
            return text;
        }
    }
}