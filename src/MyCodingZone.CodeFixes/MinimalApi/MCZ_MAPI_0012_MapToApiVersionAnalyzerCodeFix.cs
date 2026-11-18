using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyCodingZone.Analyzer;

namespace MyCodingZone.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MCZ_MAPI_0012_MapToApiVersionAnalyzerCodeFix))]
    [Shared]
    public sealed class MCZ_MAPI_0012_MapToApiVersionAnalyzerCodeFix : MinimalApiSimpleConventionCodeFixBase
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MCZ_MAPI_0012_MapToApiVersionAnalyzer.Id);

        protected override string Title => "Add .MapToApiVersion(1)";

        protected override string MethodName => "MapToApiVersion";

        protected override ArgumentListSyntax CreateArguments()
        {
            return SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)))));
        }
    }
}