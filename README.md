# CodeRules & Analyzer – Self-Defending Codebases

![Eine Codebase im Karate-Gi wehrt fehlerhafte Changes eines Entwicklers und eines AI Agents ab (AI-generiertes Bild)](self-defending-code-base.jpg)

> Für Menschen und AI Agents.
> Begleit-Repository zum Vortrag in der **MyCoding Zone** am 20.11.2026 von Rene Peuser.

Ein AI Agent kennt eure ungeschriebenen Regeln genauso wenig wie ein neuer Entwickler – er erzeugt Code nur wesentlich schneller.
Dieses Repository zeigt, wie aus einer einmal entdeckten Fehlerquelle eine **ausführbare Regel** wird, die jeden neuen Verstoß deterministisch erkennt –
vom ersten Parsen der Solution bis zur Code Rule Engine, die dieselben Regeln in jedem Repository ausführt.

## Die Story in fünf Stufen

| Stufe | Projekt | Was sie zeigt |
|---|---|---|
| **01 Solution Parsing** | `src/MyCodingZone.HowTo` | Solution und Projekte mit MSBuild lesen, C# mit Reflection und Roslyn, eine eigene kleine Abstraktion |
| **02 CodeRule als Test** | `src/MyCodingZone.CodeRules.Test` | Regeln als MSTest-Tests mit [Solution.Parser](https://www.nuget.org/packages/Solution.Parser), eigene Test-Infrastruktur (Base Class, Assert, Human-/AI-Output) |
| **03 Roslyn Analyzer** | `src/MyCodingZone.Analyzer` (+ `.Test`) | Roslyn Analyzer: Feedback direkt in IDE und Build – Async (`MCZ_ASYNC_*`), Minimal-API-Konventionen (`MCZ_MAPI_*`), Inline-Regeln (`MCZ_INLINE_*`) |
| **04 Roslyn CodeFixes** | `src/MyCodingZone.CodeFixes` (+ `.Test`) | Code Fixes zu den Analyzern: der Verstoß wird per Quick Action repariert, z. B. fehlendes `.WithTags(...)` ergänzen oder `return new X(...)` in eine lokale Variable ziehen |
| **05 Code Rule Engine** | `src/MyCodingZone.CodeRules` (+ `.Test`) | Dieselben Regeln wie Stufe 2 auf der [Siemens Code Rule Engine](https://www.nuget.org/packages/Siemens.AspNet.CodeRules.MSTest): eine Setup-Klasse, ein Test pro Regel wird generiert, Rule Pack, `RuleTester` |

Stufe 2 und Stufe 5 enthalten **dieselben zehn Regeln** mit denselben IDs. Der direkte Vergleich zeigt, was die Engine abnimmt:

| | Stufe 2: eigene Infrastruktur | Stufe 5: Siemens Code Rule Engine |
|---|---|---|
| Regel | `[TestClass]` + `[TestMethod]` + `Assert.That.CodeRuleHasNoFindings(...)` | Klasse mit `ICodeRule` / `ISolutionRule` / `IRule<TContext>` |
| Tests | von Hand geschrieben | ein Test pro Regel, **generiert** aus `CodeRuleSetup` |
| Gemeinsames Modell | Base Class mit `static Lazy<>` (`ServiceCodeRuleBase`) | eigener Context (`ServiceContext`), einmal pro Lauf erzeugt |
| Parsen | `[AssemblyInitialize]`, sequenziell | einmal pro Testlauf, Regeln laufen parallel |
| Ausgabe | eigene Human-/AI-Renderer | Human und AI eingebaut, inkl. Hinweise zum Unterdrücken |
| Ausnahmen | Whitelist im Code | `[SuppressMessage]` / `#pragma` mit Pflicht-Begründung, `SkipRule(...)` mit Scope |
| Regel testen | – | `Assert.That.CodeRule.ViolatesAsync<TRule>(…)` gegen Inline-Code mit `[\| … \|]`-Markern |
| Wiederverwenden | Code kopieren | Rule Pack (`ICodeRulePack`), läuft in MSTest, xUnit, NUnit und CLI |

## Was ist eine CodeRule?

Eine CodeRule überprüft eine projektspezifische Erwartung automatisch. Ein gutes Finding beantwortet:

| Frage | Beispiel |
|---|---|
| **Was** ist falsch? | `Example.Contracts` referenziert `Example.WebApi` |
| **Wo** liegt der Verstoß? | `src/Example.Contracts/Example.Contracts.csproj` |
| **Warum** existiert die Regel? | Contracts müssen unabhängig vom Host bleiben |
| **Wie** wird er behoben? | Referenz entfernen oder Typ nach Contracts verschieben |
| **Wie** wird der Fix validiert? | `dotnet test --filter "FullyQualifiedName~PROJECTS_RULE_001"` |

## Wo Regeln heute leben – und warum das nicht reicht

| Ort | Typisches Problem |
|---|---|
| Köpfe | Wissen verschwindet mit Personen |
| Dokumentation | Veraltet, schwer auffindbar, nicht ausführbar |
| Code Review | Spätes Feedback, wiederholte Kommentare |
| IDE-Einstellungen | Nicht verbindlich für Team, CI und Agents |
| Prompts & Skills | Kontext muss wiederholt und vollständig geliefert werden |

CodeRules liegen versioniert in der Codebase, laufen lokal und in der CI und liefern bei gleichem Input immer dasselbe Ergebnis.

## Test oder Analyzer?

| Kriterium | CodeRule als Test / Engine | CodeRule als Analyzer |
|---|---|---|
| Neue Regel | In Minuten | Höherer API- und Testaufwand |
| Zugriff | Solution, Dateien, MSBuild, Parser | C#-/VB-Semantikmodell |
| Feedback | Testlauf oder CI | Direkt in IDE und Build |
| Code Fix | Möglich, aber tricky\* | Optional automatisch |

\* Ein Code Fix im Test-Ansatz ist machbar, muss aber selbst den Quelltext an `Finding.Where` finden und ersetzen. `Finding.Current` muss dafür exakt dem Quelltext an dieser Stelle entsprechen.

Eine Regel muss nicht als Analyzer geboren werden:

1. **Beobachtung** – eine Fehlerquelle zeigt den Bedarf
2. **Lokaler Test** – die Erwartung wird schnell und deterministisch abgesichert
3. **Gemeinsame Abstraktion** – wiederkehrender Boilerplate wandert in die Engine
4. **Rule Pack** – eine allgemeine Regel gilt unverändert in mehreren Projekten
5. **Analyzer** – sofortiges IDE-Feedback rechtfertigt den Mehraufwand

## Der einfachste zuverlässige Mechanismus

| Bedarf | Mechanismus |
|---|---|
| Typen und Attribute | Reflection-Test |
| Projekt- und Paketregeln | MSBuild- oder Solution-Test |
| C#-Semantik beim Schreiben | Roslyn Analyzer |
| Automatische Reparatur | Analyzer mit Code Fix |
| XAML, SQL, JSON, Markdown | Parser + CodeRule-Test |
| Runtime-Verhalten einer API | API- oder Integrationstest |

## Stufe 3 + 4 im Detail: Analyzer und Code Fixes

| Regel | Prüft | Code Fix | In der WebApi |
|---|---|---|---|
| `MCZ_ASYNC_0001` / `0002` | `Async`-Suffix, kein `Task.WaitAll` | – | Error / Warning |
| `MCZ_MAPI_0001` / `0002` | `.Accepts<T>(mediaType)` bei POST/PUT/PATCH, keins bei GET/DELETE | ✅ | Error |
| `MCZ_MAPI_0003` / `0004` | `.Produces<TResponse>()` bzw. `.Produces(204)` passend zum Handler | ✅ | Error |
| `MCZ_MAPI_0010` / `0011` | `.WithTags(...)`, `.WithName(...)` | ✅ | Error |
| `MCZ_MAPI_0012`–`0014` | `.MapToApiVersion(...)`, `.WithDescriptionFromFile(...)`, `.WithSummaryFromFile(...)` | ✅ | aus (nicht genutzt) |
| `MCZ_MAPI_0015` | Endpoint-Handler nicht als `static` lokale Funktion | ✅ | Warning |
| `MCZ_INLINE_0001`–`0004` | kein inline `await`, Object Initializer als Argument, `return [..]`, `return new X(..)` | ✅ | Warning |

Die MAPI-Regeln sind im Analyzer `Disabled` und werden pro Repository in der `.editorconfig` eingeschaltet. Analyzer und CodeFixes hängen über `Directory.Build.props` als
`ProjectReference` mit `OutputItemType="Analyzer"` an **jedem** Projekt (außer an sich selbst): `MCZ_*` läuft in Build und IDE überall, die Code Fixes erscheinen als Quick Actions. Eine Regeländerung wirkt mit dem nächsten Build.

## Stufe 5 im Detail: Siemens Code Rule Engine

Ein Code-Rule-Projekt braucht genau **eine** Klasse:

```csharp
public sealed class CodeRuleSetup : ICodeRuleSetup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCodeRuleEngine("code-rules.slnx", options => options.AddRulePack<MyCodingZoneRulePack>());
    }
}
```

Der Source Generator aus [`Siemens.AspNet.CodeRules.MSTest`](https://www.nuget.org/packages/Siemens.AspNet.CodeRules.MSTest) liest die Registrierungen beim Build und schreibt einen MSTest-Test pro Regel
(`RECORDS_RULE_001_RecordPropertiesMustBeImmutable`, ...), gruppiert nach `Projects`, `Records` und `Services`.
Ein Guard-Test schlägt fehl, wenn eine registrierte Regel keinen Test bekommen hat.

| Paket | Zweck |
|---|---|
| [`Siemens.AspNet.CodeRules.Contracts`](https://www.nuget.org/packages/Siemens.AspNet.CodeRules.Contracts) | Regeln und Rule Packs schreiben (`ICodeRule`, `Finding`, `CodeContext`, `SolutionContext`) |
| [`Siemens.AspNet.CodeRules.Engine`](https://www.nuget.org/packages/Siemens.AspNet.CodeRules.Engine) | `ICodeRuleSetup`, Registrierung, `RuleTester<TRule>` |
| [`Siemens.AspNet.CodeRules.MSTest`](https://www.nuget.org/packages/Siemens.AspNet.CodeRules.MSTest) | bringt beides mit, plus Source Generator |

Die Engine achtet auf die Qualität der Findings: `Current` muss exakt der Quelltext an `Where` sein, weil ein Fix genau diesen Bereich durch `ShouldBe` ersetzt.
Beim Portieren hat `RuleTester` genau solche Ungenauigkeiten der Stufe-2-Regeln aufgedeckt (z. B. `SERVICES_RULE_003` meldete die Klasse statt des Parameters).

## Die Beispiel-API: Siemens Enterprise Error Handler

`src/MyCodingZone.WebApi` ist eine Minimal API, gegen die die Regeln laufen. Fehler beantwortet sie mit
[`Siemens.AspNet.ErrorHandling`](https://www.nuget.org/packages/Siemens.AspNet.ErrorHandling) mit [`Siemens.AspNet.ErrorHandling.Contracts`](https://www.nuget.org/packages/Siemens.AspNet.ErrorHandling.Contracts) (RFC 7807):

```csharp
builder.Services.AddErrorHandling(builder.Configuration);
// ...
app.UseErrorHandling();
```

Ein ungültiger Request liefert ein `ValidationProblemDetailsExtended` mit der betroffenen Property, dem aktuellen Wert, den erlaubten Werten und der Position im JSON.
Der Client kann seinen Request also selbst korrigieren:

```json
{
  "type": "ValidationProblemDetailsExtended",
  "title": "The JSON request body contains invalid properties",
  "status": 422,
  "errorDetails": {
    "userType": {
      "currentValue": "Organizer",
      "samples": [ "Visitor", "Developer", "Speaker" ],
      "location": { "row": 5, "column": 25 }
    }
  }
}
```

Die vollständigen Antworten liegen als Snapshots in `src/MyCodingZone.WebApi.Test/Api/Users/V1/Create/InvalidRequests/Responses/`.

## Repository-Struktur

```
src/
├── MyCodingZone.HowTo/            01 Einstieg: MSBuild, Reflection, Roslyn und eine eigene Abstraktion
├── MyCodingZone.CodeRules.Test/  02 CodeRules als Tests (eigene Infrastruktur)
├── MyCodingZone.Analyzer/         03 Roslyn Analyzer (MCZ_ASYNC_*, MCZ_MAPI_*, MCZ_INLINE_*)
├── MyCodingZone.Analyzer.Test/   03 Spezifikation des Analyzers, ein Szenario pro Datei
├── MyCodingZone.CodeFixes/        04 Code Fixes zu den Analyzern (MCZ_MAPI_*, MCZ_INLINE_*)
├── MyCodingZone.CodeFixes.Test/  04 Spezifikation der Code Fixes: Quelltext vorher / nachher, ein Szenario pro Datei
├── MyCodingZone.CodeRules/        05 Dieselben Regeln auf der Siemens Code Rule Engine (Setup, Rule Pack, generierte Tests)
├── MyCodingZone.CodeRules.Engine.Test/   05 Funktionale Tests der Regeln mit Assert.That.CodeRule, gleiche Ordner wie die Regeln (<Regel>_Tests.cs)
├── MyCodingZone.WebApi/           Beispiel-API (Minimal API) mit Siemens Error Handler
└── MyCodingZone.WebApi.Test/     Snapshot-basierte API-Tests, inkl. Fehlerfälle
```

## Voraussetzungen

- [.NET SDK 10.0.404](https://dotnet.microsoft.com/download) (fixiert über `global.json`)
- Visual Studio 2026, JetBrains Rider oder VS Code mit C# Dev Kit
- [Git LFS](https://git-lfs.com) für Folien und Bilder (`git lfs install` vor dem Klonen)

## Loslegen

```bash
dotnet build code-rules.slnx

# alle Tests
dotnet test code-rules.slnx

# alle CodeRules (Stufe 2 und Stufe 5)
dotnet test code-rules.slnx --filter TestCategory=CodeRules

# nur die Engine (Stufe 5), eine Gruppe oder ein Tag
dotnet test src/MyCodingZone.CodeRules --filter "TestCategory=CodeRules Services"
dotnet test src/MyCodingZone.CodeRules --filter "TestCategory=CodeRules DependencyInjection"

# die funktionalen Tests der Regeln (Assert.That.CodeRule)
dotnet test src/MyCodingZone.CodeRules.Engine.Test

# nur die HowTo-Beispiele
dotnet test src/MyCodingZone.HowTo --filter TestCategory=HowTo
```

### Demo-Modus vs. CI-Modus

`Directory.Build.props` ist lokal auf Geschwindigkeit optimiert: nur die eigenen `MCZ_*`-Analyzer laufen, die .NET-Analyzer sind aus, Warnings blockieren nicht.
Mit `CI=true` greift der strikte Modus: alle .NET-Analyzer, Code-Style im Build, NuGet-Audit und `TreatWarningsAsErrors`.

```bash
dotnet build code-rules.slnx -p:CI=true
```

### Versionen, die zusammengehören

| Paket | Version | Grund |
|---|---|---|
| `Microsoft.CodeAnalysis.*` | 5.9.0 | Compiler des fixierten SDK 10.0.404. Ein neuerer Analyzer baut zwar, wird vom Compiler aber nicht geladen (CS9057). |
| `Microsoft.Build*` | 18.9.6 | Letzte Version mit `net10.0`-Target. Ab 18.10 gibt es nur noch `net11.0`. |

Beide gemeinsam mit `global.json` anheben.

## CodeRules vs. AI

| AI / LLM | CodeRules |
|---|---|
| Findet neue, unbekannte Probleme | Überwacht bekannte Erwartungen |
| Benötigt ausreichend Kontext | Regel liegt versioniert in der Codebase |
| Ergebnisse können variieren | Gleicher Input, gleiches Ergebnis |
| Kosten pro Lauf (Tokens / Rechenleistung) | Nach der Implementierung günstig |
| Stark bei Analyse und Reparatur | Stark bei verbindlicher Absicherung |

Entwickler und AI Agents ändern den Code. CodeRules prüfen deterministisch. Beide lesen das Finding, korrigieren und führen dieselben Regeln erneut aus.
`CodeRuleSettings__OutputMode=ai` schaltet die Ausgabe beider Regel-Stufen auf JSON für Agents um.

## Einführung in bestehende Codebases

1. **Baseline** – bestehende Verstöße erfassen
2. **Neue Verstöße** – neue Fehler zuerst blockieren
3. **Schweregrad** – Analyzer von Hinweis über Warning zu Error entwickeln
4. **Ausnahmen** – begründen und mit Ablaufdatum versehen
5. **Pflege** – Nutzen, Laufzeit und False Positives regelmäßig prüfen

## AI Agents

[`AGENTS.md`](AGENTS.md) beschreibt Befehle, Struktur und Regeln für AI Agents (Codex, Copilot, Cursor u. a.).
[`CLAUDE.md`](CLAUDE.md) importiert sie für Claude Code. Die Testprojekte des Analyzers haben eigene `AGENTS.md` mit ihren Spezifikationsregeln.

## Folien

Die Präsentation liegt als [`MyCoding_Zone_CodeRules_Analyzer.pptx`](MyCoding_Zone_CodeRules_Analyzer.pptx) im Repository (Git LFS).

## Autor

**Rene Peuser** – Independent Software Developer, C# / .NET / ASP.NET Core

- GitHub: [RenePeuser](https://github.com/RenePeuser)
- LinkedIn: [rene-peuser](https://linkedin.com/in/rene-peuser)
- NuGet: [Rene.Peuser](https://www.nuget.org/profiles/Rene.Peuser)

## Lizenz

[MIT](LICENSE)
