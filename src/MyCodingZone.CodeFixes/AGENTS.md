# AI Instructions – MyCodingZone.CodeFixes

This project contains Roslyn CodeFix providers.

CodeFixes modify user code.
Safety and minimal change are mandatory.

## Structure & Naming (MANDATORY)

CodeFixes MUST mirror analyzer structure and naming.

### Folder Structure

- Folder structure MUST match the analyzer domain structure
- Infrastructure helpers MUST be separated from CodeFix providers
- Infrastructure folders MUST NOT contain CodeFix providers

### CodeFix File Naming

CodeFix files MUST follow this pattern:

MCZ_<DOMAIN>_<NUMBER>_<AnalyzerName>CodeFix.cs

Example:
MCZ_MAPI_0020_ProducesResponseDtoCodeFix.cs

### CodeFix Class Naming

CodeFix classes MUST match the file name:

MCZ_MAPI_0020_ProducesResponseDtoCodeFix

### Diagnostic Mapping Rules

- Exactly one CodeFix per DiagnosticId
- CodeFix DiagnosticId MUST match analyzer DiagnosticId
- CodeFix MUST NOT register for multiple DiagnosticIds

## Mandatory Rules

- CodeFix must ONLY fix what the diagnostic describes
- Preserve formatting, trivia, and comments
- Prefer minimal syntactic change

## Design Rules

- One CodeFix per diagnostic
- Use SyntaxGenerator where possible
- Do not reorder unrelated code
- Do not introduce additional behavior

## Technical Constraints

- CodeFix must work for all diagnostic locations
- CodeFix must not assume formatting or line layout
- CodeFix must support different code shapes

## Forbidden

- Aggressive refactorings
- Multi-step fixes without user intent
- Removing or rewriting comments
- Formatting-only changes

## Test Contract

Every CodeFix MUST have:
- Explicit input source
- Explicit expected output source
- Verification that only intended code changed
