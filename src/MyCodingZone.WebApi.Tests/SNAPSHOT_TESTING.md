# Snapshot Testing mit AspNetCore.Simple.MsTest.Sdk 📸

## 🎯 Konzept

Die API-Tests nutzen [AspNetCore.Simple.MsTest.Sdk](https://www.nuget.org/packages/AspNetCore.Simple.MsTest.Sdk).
Ein Test schickt einen Request aus einer JSON-Datei an die API und vergleicht die komplette Antwort
(Status-Code, Header, Body) mit einer zweiten JSON-Datei, dem Snapshot.

Seit SDK 10.1 gibt es zwei gleichwertige Schreibweisen:

- **Klassische API**: `Client.AssertPostAsync<T>(url, request, response)`, ein Aufruf mit Parametern.
- **Fluent API**: `Client.AssertPost(url).Accepts…().Produces<T>().ExpectedResponse…().ExecuteAsync()`,
  liest sich wie der Endpoint-Vertrag.

Beide laufen durch dieselbe Pipeline und liefern dieselbe Fehlerausgabe. Jeder Testfall existiert deshalb
in beiden Varianten, mit denselben Snapshots, damit man sie direkt vergleichen kann.

## 📁 Ordnerstruktur

```
Api/Users/V1/Create/
├── Showcase/                         ← erfolgreiche Requests
│   ├── Show_Case_Test.cs             ← klassische API
│   ├── Show_Case_Fluent_Test.cs      ← Fluent API, dieselben Testfälle
│   ├── Requests/
│   │   └── MyCodingZone_User.json
│   └── Responses/
│       └── MyCodingZone_User.json
└── InvalidRequests/                  ← Fehlerfälle (Siemens Error Handler)
    ├── Invalid_Request_Test.cs
    ├── Requests/
    │   ├── Missing_FirstName.json
    │   └── Unknown_UserType.json
    └── Responses/
        ├── Missing_FirstName.json
        └── Unknown_UserType.json
```

## 🔥 Klassisch vs. Fluent

```csharp
// Klassisch
[TestMethod]
[DynamicRequestLocator]
public Task Should_Be_Able_To_Create_A_New_User(string useCase)
{
    return Client.AssertPostAsync<CreateUserResponse>("api/v1/users",
                                                      useCase,
                                                      useCase);
}
```

```csharp
// Fluent
[TestMethod]
[DynamicRequestLocator]
public Task Should_Be_Able_To_Create_A_New_User(string useCase)
{
    return Client.AssertPost("api/v1/users")
                 .AcceptsFromEmbeddedJson(useCase)
                 .Produces<CreateUserResponse>(HttpStatusCode.OK)
                 .ExpectedResponseFromEmbeddedJson(useCase)
                 .ExecuteAsync();
}
```

**Was passiert** (in beiden Varianten gleich):
1. `[DynamicRequestLocator]` findet alle Dateien in `Requests/` und startet den Test einmal pro Datei,
   z. B. `Should_Be_Able_To_Create_A_New_User("MyCodingZone_User.json")`.
2. Der Request-Body wird per POST an `api/v1/users` geschickt.
3. Die Antwort wird mit `Responses/MyCodingZone_User.json` verglichen. Unterschiede erscheinen als Tabelle mit Pfad,
   erwartetem und aktuellem Wert, dazu ein curl-Befehl zum Nachstellen.

**Was die Fluent API zusätzlich bringt:**
- Der erwartete Status-Code steht explizit im Test (`Produces<T>(HttpStatusCode.OK)`).
- Der Builder führt: `Accepts…` gibt es nur bei POST/PUT/PATCH/QUERY und nur einmal, Vergleichsoptionen erst
  nach `ExpectedResponse…`. Falsche Ketten kompilieren nicht.
- Ein vergessenes `ExecuteAsync()` ist ein Build-Fehler (Analyzer `MSTESTSDK001`), kein grüner Test.
- Gezielte Ignores pro Test: `.IgnoreProperty(r => r.User.Id)` oder
  `.IgnoreDifferencesWhere(d => …)`.
- Request-Daten pro Test: `.WithParameter("Name", "MyCodingZone")` für `$Name$`-Platzhalter, `.WithQueryParameter(…)`,
  `.WithHeader(…)`, `.WithBearerToken(…)`.

## 📸 Snapshot-Format

`Requests/MyCodingZone_User.json` ist der reine Request-Body:

```json
{
  "firstName": "MyCodingZone",
  "lastName": "2026",
  "email": "mycodingzone@2026.local",
  "userType": "Developer"
}
```

`Responses/MyCodingZone_User.json` enthält die ganze Antwort:

```json
{
  "content": {
    "headers": [ { "key": "Content-Type", "value": [ "application/json; charset=utf-8" ] } ],
    "value": {
      "user": {
        "id": "c0daeb10-f423-4ab7-bb84-3d7789b9f6d2",
        "firstName": "MyCodingZone",
        "lastName": "2026",
        "email": "mycodingzone@2026.local",
        "userType": "Developer"
      }
    }
  },
  "statusCode": "OK",
  "headers": [],
  "trailingHeaders": [],
  "isSuccessStatusCode": true
}
```

Die `id` erzeugt die API bei jedem Aufruf neu. `ApiTestBase` ignoriert sie global über die `DifferenceFunc`,
deshalb darf im Snapshot eine beliebige Guid stehen.

## ⚙️ Globale Einstellungen (`ApiTestBase`)

```csharp
services.AddAssertableHttpClient(configuration, settings =>
{
    settings.WriteResponse = true;            // Snapshot bei Abweichung neu schreiben
    settings.DifferenceFunc = DifferenceFunc; // ignoriert jede "id"
});
```

`WriteResponse = true` heißt: Weicht die Antwort ab, schlägt der Test fehl **und** die echte Antwort wird in den
Snapshot geschrieben. Der nächste Lauf ist grün. Die Änderung deshalb immer im Git-Diff prüfen, bevor sie
committet wird. In der Fluent API gibt es dafür pro Test `.WriteSnapshot()`; der Analyzer `MSTESTSDK003` warnt im
Release-Build, falls es im Code stehen bleibt.

## ➕ Neuen Testfall anlegen

1. `Requests/<UseCase>.json` mit dem Request-Body anlegen.
2. Tests laufen lassen. Der neue Fall schlägt einmal fehl und schreibt `Responses/<UseCase>.json`.
3. Snapshot prüfen, beide Dateien committen. Klassischer und Fluent-Test nehmen den Fall automatisch mit.

Fehlerfälle brauchen eine eigene Testmethode mit eigenem `Requests/`-Ordner, weil die Methoden oben eine
erfolgreiche Antwort erwarten. Siehe `InvalidRequests/`: ein unbekannter `userType` und ein fehlendes Pflichtfeld.
Die API beantwortet beide über den Siemens Enterprise Error Handler (`Siemens.AspNet.ErrorHandling`) mit
**422 `ValidationProblemDetailsExtended`**: betroffene Property, aktueller Wert, erlaubte Werte (`samples`) und
die Position im JSON (`location`).

```csharp
return Client.AssertPostAsErrorAsync<ValidationProblemDetailsExtended>("api/v1/users",
                                                                       useCase,
                                                                       useCase);
```

Der Typ muss zum Endpoint-Vertrag passen: Der Endpoint deklariert
`.Produces<ValidationProblemDetailsExtended>(StatusCodes.Status422UnprocessableEntity)`. Mit `ProblemDetails`
bricht das SDK schon vor dem HTTP-Call mit „HTTP RESPONSE TYPE MISMATCH“ ab. Die Fluent-Variante für
Fehlerantworten steht noch auf der Roadmap des SDK.

## 🚀 Tests ausführen

```bash
# Alle API-Tests
dotnet test src/MyCodingZone.WebApi.Tests

# Nur die Fluent-Variante
dotnet test src/MyCodingZone.WebApi.Tests --filter "FullyQualifiedName~Fluent"

# Ausgabe als JSON für AI-Tools statt der gerahmten Ausgabe
TestSdkSettings__OutputMode=ai dotnet test src/MyCodingZone.WebApi.Tests
```

## 🔧 Troubleshooting

**Test schlägt mit einer Differenz-Tabelle fehl**
Hat sich die API geändert, ist der neue Snapshot schon geschrieben (`WriteResponse = true`). Diff prüfen,
committen, Test erneut laufen lassen.

**Der Status-Code passt nicht**
Die Methode erwartet einen anderen Status-Code als die API liefert. Für Fehlerfälle eine eigene Testmethode
anlegen (siehe oben).

**Request-Datei wird nicht gefunden**
Der `useCase` ist der Dateiname inklusive `.json`. Die Datei muss im `Requests/`-Ordner neben der Testklasse
liegen und als Embedded Resource eingebunden sein (passiert über `MyCodingZone.WebApi.Tests.csproj` automatisch).

## 📚 Weitere Infos

- [AspNetCore.Simple.MsTest.Sdk auf NuGet](https://www.nuget.org/packages/AspNetCore.Simple.MsTest.Sdk#readme-body-tab)
- [RFC 9457 - Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457)
