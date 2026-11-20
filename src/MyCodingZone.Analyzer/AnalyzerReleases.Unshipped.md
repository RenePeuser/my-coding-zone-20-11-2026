; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID          | Category | Severity | Notes
-----------------|----------|----------|-------
MCZ_ASYNC_0001 | Async.Design | Error | Methods returning Task/ValueTask must be named with an 'Async' suffix
MCZ_ASYNC_0002 | Async.Design | Warning | Do not call Task.WaitAll
MCZ_INLINE_0001 | Readability | Warning | Do not inline an await expression into another expression
MCZ_INLINE_0002 | Readability | Warning | Do not pass an object initializer inline as an argument
MCZ_INLINE_0003 | Readability | Warning | Do not return a non-empty collection expression inline
MCZ_INLINE_0004 | Readability | Warning | Do not return a new object creation inline
MCZ_MAPI_0015 | MinimalApi.Design | Info | Endpoint handlers must not be static

### Changed Rules

Rule ID          | New Category      | New Severity | Old Category      | Old Severity | Notes
-----------------|-------------------|--------------|-------------------|--------------|-------
MCZ_MAPI_0001 | MinimalApi.Design | Info | MinimalApi.Design | Disabled | Enabled by default, Visual Studio skips disabled analyzers in live analysis
MCZ_MAPI_0002 | MinimalApi.Design | Info | MinimalApi.Design | Disabled | Enabled by default, Visual Studio skips disabled analyzers in live analysis
MCZ_MAPI_0003 | MinimalApi.Design | Info | MinimalApi.Design | Disabled | Enabled by default, Visual Studio skips disabled analyzers in live analysis
MCZ_MAPI_0004 | MinimalApi.Design | Info | MinimalApi.Design | Disabled | Enabled by default, Visual Studio skips disabled analyzers in live analysis
MCZ_MAPI_0010 | MinimalApi.Design | Info | MinimalApi.Design | Disabled | Enabled by default, Visual Studio skips disabled analyzers in live analysis
MCZ_MAPI_0011 | MinimalApi.Design | Info | MinimalApi.Design | Disabled | Enabled by default, Visual Studio skips disabled analyzers in live analysis
