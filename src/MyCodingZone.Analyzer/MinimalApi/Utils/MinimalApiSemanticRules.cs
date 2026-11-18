using Microsoft.CodeAnalysis;

namespace MyCodingZone.Analyzer
{
    internal static class MinimalApiSemanticRules
    {
        public static bool RequiresAccepts(string? mapMethodName)
        {
            return mapMethodName is "MapPost" or "MapPut" or "MapPatch";
        }

        public static bool ForbidsAccepts(string? mapMethodName)
        {
            return mapMethodName is "MapGet" or "MapDelete";
        }

        public static bool IsNoContentReturnType(ITypeSymbol? returnType)
        {
            if (returnType is null)
            {
                return false;
            }

            // Task / ValueTask (non-generic)
            if (returnType is INamedTypeSymbol named && !named.IsGenericType)
            {
                return named.ToDisplayString() is "System.Threading.Tasks.Task" or "System.Threading.Tasks.ValueTask";
            }

            // IResult
            return returnType.ToDisplayString() == "Microsoft.AspNetCore.Http.IResult";
        }
    }
}