# AI Instructions – MyCodingZone.Analyzer

This project contains Roslyn analyzers.

Analyzers are long-lived compiler components.
Correctness and stability are more important than minimal code.

### Folder Structure

Analyzers MUST be organized by domain:

- ArgumentCheck
- Inline
- MinimalApi
- (future domains follow the same pattern)

Infrastructure code MUST be separated from analyzers:

- Infrastructure folders contain:
  - shared helpers
  - walkers
  - semantic utilities
- Infrastructure folders MUST NOT contain analyzers
- Infrastructure classes MUST NOT have DiagnosticIds
- Infrastructure classes MUST NOT derive from DiagnosticAnalyzer
  and MUST NOT be decorated with [DiagnosticAnalyzer]

### Analyzer File Naming

Analyzer files MUST follow this pattern:

MCZ_<DOMAIN>_<NUMBER>_<AnalyzerName>Analyzer.cs

Example:
MCZ_MAPI_0020_ProducesResponseDtoAnalyzer.cs

### Analyzer Class Naming

Analyzer classes MUST match the file name:

MCZ_MAPI_0020_ProducesResponseDtoAnalyzer

Base classes and shared analyzer abstractions
MUST NOT contain DiagnosticIds
and MUST NOT include the DiagnosticId in their class name.

### DiagnosticId Rules

- DiagnosticId MUST be identical to the ID in the class/file name
- DiagnosticIds are NEVER reused
- Deleted analyzers keep their IDs reserved

## Mandatory Rules

- Specification BEFORE implementation
- Enumerate real-world edge cases explicitly
- Prefer semantic analysis over syntax analysis
- Assume analyzers will run on unknown customer code

## Design Rules

- One analyzer = one responsibility
- One diagnostic = one clear rule
- Analyzer behavior must be deterministic
- Performance matters (runs during compilation)

## Technical Constraints

- Prefer Symbol / Operation based analysis
- Do NOT rely on InvocationExpressionSyntax alone
- Do NOT compare method names as strings
- Do NOT assume a single code shape

## Forbidden

- Monster analyzers
- Hidden assumptions in helper methods
- Logic that is not covered by tests

## Test Contract

Every analyzer MUST have:
- Explicit scenario-based tests
- Regression tests for every fixed bug
- Clear separation between valid and invalid cases
