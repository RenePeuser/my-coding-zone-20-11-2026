using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyCodingZone.Analyzer
{
    public static class MinimalApiHandlerInference
    {
        public static ITypeSymbol? TryGetHandlerReturnType(MinimalApiInvocationChain chain,
                                                           SemanticModel semanticModel,
                                                           CancellationToken ct)
        {
            // MapX("route", HandleAsync) -> last argument is handler expression
            var args = chain.RootMapInvocation.ArgumentList.Arguments;

            if (args.Count == 0)
            {
                return null;
            }

            var handlerExpr = args.Last().Expression;

            var symbol = semanticModel.GetSymbolInfo(handlerExpr, ct).Symbol;

            if (symbol is IMethodSymbol method)
            {
                return method.ReturnType;
            }

            if (symbol is ILocalSymbol local && local.Type is INamedTypeSymbol named && named.DelegateInvokeMethod is not null)
            {
                return named.DelegateInvokeMethod.ReturnType;
            }

            // Fallback for test compilations without ASP.NET Core references:
            // infer return type from a local function declaration in the same method body.
            if (handlerExpr is IdentifierNameSyntax id)
            {
                var methodDecl = chain.RootMapInvocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();

                if (methodDecl?.Body is not null)
                {
                    var localFunc = methodDecl.Body.DescendantNodes()
                                              .OfType<LocalFunctionStatementSyntax>()
                                              .FirstOrDefault(l => string.Equals(l.Identifier.ValueText, id.Identifier.ValueText, StringComparison.Ordinal));

                    if (localFunc is not null)
                    {
                        return semanticModel.GetTypeInfo(localFunc.ReturnType, ct).Type;
                    }
                }

                // Local functions can also be declared in expression-bodied members.
                if (methodDecl?.ExpressionBody is not null)
                {
                    var localFunc = methodDecl.ExpressionBody.Expression
                                              .DescendantNodes()
                                              .OfType<LocalFunctionStatementSyntax>()
                                              .FirstOrDefault(l => string.Equals(l.Identifier.ValueText, id.Identifier.ValueText, StringComparison.Ordinal));

                    if (localFunc is not null)
                    {
                        return semanticModel.GetTypeInfo(localFunc.ReturnType, ct).Type;
                    }
                }
            }

            return null;
        }

        public static ITypeSymbol? TryGetResponseDtoType(ITypeSymbol? returnType)
        {
            if (returnType is null)
            {
                return null;
            }

            // Task<T>
            if (returnType is INamedTypeSymbol named && named.IsGenericType)
            {
                if (named.ToDisplayString().StartsWith("System.Threading.Tasks.Task<", StringComparison.Ordinal) ||
                    named.ToDisplayString().StartsWith("System.Threading.Tasks.ValueTask<", StringComparison.Ordinal))
                {
                    return named.TypeArguments[0];
                }
            }

            // Non-generic types are treated as no DTO.
            return null;
        }

        public static bool HasConcreteResponseDto(ITypeSymbol? returnType)
        {
            var dto = TryGetResponseDtoType(returnType);

            if (dto is not null && dto.Name == "IResult" && dto.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Http")
            {
                return false;
            }

            return dto is not null && dto.SpecialType == SpecialType.None;
        }
    }
}