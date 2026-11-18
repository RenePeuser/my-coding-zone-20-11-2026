using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_MAPI_0013_WithDescriptionFromFileAnalyzerCodeFix))]
    [Shared]
    public sealed class MCZ_MAPI_0013_WithDescriptionFromFileAnalyzerCodeFix : MinimalApiSimpleConventionCodeFixBase
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_MAPI_0013_WithDescriptionFromFileAnalyzer.Id);

        protected override string Title => "Add .WithDescriptionFromFile(\"Description.md\")";

        protected override string MethodName => "WithDescriptionFromFile";

        protected override ArgumentListSyntax CreateArguments()
        {
            return SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                                                                                                                          SyntaxFactory.Literal("Description.md")))));
        }
    }
}