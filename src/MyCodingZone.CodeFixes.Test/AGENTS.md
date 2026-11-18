# AI Instructions – CodeFix Tests

This project defines the SPECIFICATION of CodeFix behavior.
Tests document user-visible code changes.

## Core Principles

- ONE CODEFIX SCENARIO = ONE TEST FILE
- ONE DIAGNOSTIC RULE = ONE TEST FOLDER
- A CODEFIX MAY HAVE MULTIPLE SCENARIOS

## Test Folder Structure (MANDATORY)

CodeFix tests MUST be organized by DiagnosticId.

Structure:
<Domain>/<DiagnosticId>_<RuleName>/<Scenario>.cs

Example:
MinimalApi/MCZ_MAPI_0020_ProducesResponseDto/MapGet_TaskOfDto_AddsProduces.cs

Rules:
- One DiagnosticId per folder
- Multiple CodeFix scenarios per DiagnosticId folder are allowed
- One scenario per test file
- Test folder MUST map 1:1 to exactly one DiagnosticId
- Scenario file name describes the fix behavior

## Mandatory Rules

- One scenario per test file
- One TestClass per scenario
- One TestMethod per scenario
- File name MUST describe the CodeFix behavior

Example:
MapGet_TaskOfDto_AddsProduces.cs

## Scenario Rules

Each scenario MUST clearly express:
- The triggering diagnostic
- The code before the fix
- The expected code after the fix
- Whether the fix is offered or not

Scenarios MUST cover:
- Standard fix cases
- Already-correct code (no-op)
- Edge cases (different formatting / layouts)
- Ambiguous cases where no fix should be offered

## Test Design Rules

- Input source and expected output MUST be explicit
- Expected output MUST show minimal change
- Only the diagnostic target may change
- Formatting, trivia, and comments MUST be preserved

## Required Coverage Dimensions

Scenarios MUST vary across:
- Different code shapes producing the same diagnostic
- Different formatting and line layouts
- Multiple diagnostics where only one is fixed
- Cases where the fix must NOT be offered

## Forbidden

- Monster test classes
- Grouping multiple scenarios in one file
- Multiple DiagnosticIds in one test folder
- Snapshot-style mega tests
- Partial output verification
- Implicit expectations

## Regression Rule

Every fixed CodeFix bug MUST result in:
- A new dedicated CodeFix scenario test file
- Located in the folder of the affected DiagnosticId
- With a descriptive file name explaining the fix
