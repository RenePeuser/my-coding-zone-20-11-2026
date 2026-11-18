using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyCodingZone.Analyzer
{
    public readonly struct MinimalApiInvocationChain(InvocationExpressionSyntax rootMapInvocation,
                                                     ImmutableArray<InvocationExpressionSyntax> invocations,
                                                     IMethodSymbol? rootMapSymbol) : IEquatable<MinimalApiInvocationChain>
    {
        public InvocationExpressionSyntax RootMapInvocation { get; } = rootMapInvocation;

        public ImmutableArray<InvocationExpressionSyntax> Invocations { get; } = invocations;

        public IMethodSymbol? RootMapSymbol { get; } = rootMapSymbol;

        public string? RootMapMethodName => RootMapSymbol?.Name ?? GetInvokedMethodName(RootMapInvocation);

        public InvocationExpressionSyntax? FindInvocation(string methodName)
        {
            return Invocations.FirstOrDefault(i => GetInvokedMethodSymbolName(i) == methodName);
        }

        private static string? GetInvokedMethodSymbolName(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return null;
            }

            return memberAccess.Name.Identifier.ValueText;
        }

        private static string? GetInvokedMethodName(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
                _ => null
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is MinimalApiInvocationChain other && Equals(other);
        }

        public bool Equals(MinimalApiInvocationChain other)
        {
            return
                EqualityComparer<InvocationExpressionSyntax>.Default.Equals(RootMapInvocation, other.RootMapInvocation) &&
                Invocations.SequenceEqual(other.Invocations) &&
                SymbolEqualityComparer.Default.Equals(RootMapSymbol, other.RootMapSymbol);
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = (hash * 31) + (RootMapInvocation != null ? RootMapInvocation.GetHashCode() : 0);

            foreach (var invocation in Invocations)
            {
                hash = (hash * 31) + (invocation != null ? invocation.GetHashCode() : 0);
            }

            hash = (hash * 31) + (RootMapSymbol != null ? SymbolEqualityComparer.Default.GetHashCode(RootMapSymbol) : 0);

            return hash;
        }

        public static bool operator ==(MinimalApiInvocationChain left,
                                       MinimalApiInvocationChain right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MinimalApiInvocationChain left,
                                       MinimalApiInvocationChain right)
        {
            return !(left == right);
        }
    }
}