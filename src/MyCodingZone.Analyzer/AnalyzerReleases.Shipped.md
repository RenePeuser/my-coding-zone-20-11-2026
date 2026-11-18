; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID        | Category          | Severity | Notes
---------------|-------------------|----------|-------------------------------------------------------------------------------
MCZ_MAPI_0001 | MinimalApi.Design | Disabled | Missing required `.Accepts<T>(mediaType)` for POST/PUT/PATCH
MCZ_MAPI_0002 | MinimalApi.Design | Disabled | Forbidden `.Accepts<T>(...)` for GET/DELETE
MCZ_MAPI_0003 | MinimalApi.Design | Disabled | Missing `.Produces<TResponse>()` for returned DTO
MCZ_MAPI_0004 | MinimalApi.Design | Disabled | Missing `.Produces(StatusCodes.Status204NoContent)` for no-content handlers
MCZ_MAPI_0010 | MinimalApi.Design | Disabled | Missing `.WithTags(...)`
MCZ_MAPI_0011 | MinimalApi.Design | Disabled | Missing `.WithName(...)`
MCZ_MAPI_0012 | MinimalApi.Design | Disabled | Missing `.MapToApiVersion(...)`
MCZ_MAPI_0013 | MinimalApi.Design | Disabled | Missing `.WithDescriptionFromFile(...)`
MCZ_MAPI_0014 | MinimalApi.Design | Disabled | Missing `.WithSummaryFromFile(...)`

