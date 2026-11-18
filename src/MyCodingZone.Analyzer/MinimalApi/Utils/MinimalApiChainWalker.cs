using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyCodingZone.Analyzer
{
    public static class MinimalApiChainWalker
    {
        public static MinimalApiInvocationChain? TryCreate(InvocationExpressionSyntax? anyInvocation,
                                                           SemanticModel semanticModel,
                                                           CancellationToken ct)
        {
            if (anyInvocation is null)
            {
                return null;
            }

            // We want the *root* Map* call and then every chained invocation after it.
            // Find the Map* invocation even if analysis is triggered on a chained call (e.g. `.Produces(...)`).
            var rootMapInvocation = FindRootMapInvocation(anyInvocation);

            if (rootMapInvocation is null)
            {
                return null;
            }

            var symbol = semanticModel.GetSymbolInfo(rootMapInvocation, ct).Symbol;

            if (symbol is IMethodSymbol rootSymbol)
            {
                if (!IsMapMethodName(rootSymbol.Name))
                {
                    return null;
                }

                var boundInvocationChain = CollectOutwardInvocations(rootMapInvocation);

                return new MinimalApiInvocationChain(rootMapInvocation, boundInvocationChain, rootSymbol);
            }

            // Fallback for test compilations that don't reference ASP.NET Core.
            // We still want analyzers to work based on syntax alone.
            var rootMethodName = GetInvokedMemberName(rootMapInvocation);

            if (!IsMapMethodName(rootMethodName))
            {
                return null;
            }

            // Then walk outward to collect the full chain: Map*().Accepts()... etc.
            var unboundInvocationChain = CollectOutwardInvocations(rootMapInvocation);

            return new MinimalApiInvocationChain(rootMapInvocation, unboundInvocationChain, null);
        }

        private static bool IsMapMethodName(string? methodName)
        {
            return methodName is "MapGet" or "MapPost" or "MapPut" or "MapPatch" or "MapDelete";
        }

        private static string? GetInvokedMemberName(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
                _ => null
            };
        }

        private static InvocationExpressionSyntax? FindRootMapInvocation(InvocationExpressionSyntax start)
        {
            // Walk the member-access chain inward until we reach a Map* invocation.
            var current = start;

            while (true)
            {
                var candidateName = GetInvokedMemberName(current);

                if (IsMapMethodName(candidateName))
                {
                    return current;
                }

                if (current.Expression is MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax inner })
                {
                    current = inner;

                    continue;
                }

                return null;
            }
        }

        private static ImmutableArray<InvocationExpressionSyntax> CollectOutwardInvocations(InvocationExpressionSyntax root)
        {
            var builder = ImmutableArray.CreateBuilder<InvocationExpressionSyntax>();
            builder.Add(root);

            SyntaxNode current = root;

            while (true)
            {
                // handles: root.MapPost(...).Accepts(...)
                if (current.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax nextInvocation })
                {
                    builder.Add(nextInvocation);
                    current = nextInvocation;

                    continue;
                }

                // handles: root?.Accepts(...)
                if (current.Parent is ConditionalAccessExpressionSyntax { Parent: InvocationExpressionSyntax nextConditional })
                {
                    builder.Add(nextConditional);
                    current = nextConditional;

                    continue;
                }

                break;
            }

            return builder.ToImmutable();
        }
    }
}