# Mitwirken

Danke für dein Interesse am OpSec Audit Tool.

## Entwicklungsumgebung

- .NET SDK 10
- Linux oder Windows
- optional Visual Studio Code mit der C#-Erweiterung

```bash
dotnet restore
dotnet build OpSecAuditTool.csproj --configuration Debug
```

Vor einem Pull Request bitte außerdem prüfen:

```bash
dotnet format OpSecAuditTool.csproj --verify-no-changes
dotnet build OpSecAuditTool.csproj --configuration Release
```

## Änderungen

- Eine Änderung pro Pull Request erleichtert die Prüfung.
- Neue Audit-Checks implementieren `IOpSecChecker` und werden in
  `Core/AuditCheckerCatalog.cs` registriert.
- Nicht ausführbare oder übersprungene Prüfungen dürfen nicht als bestanden
  gewertet werden.
- Plattformabhängiger Code muss Linux und Windows sauber unterscheiden.
- Neue Netzwerkzugriffe müssen standardmäßig deaktiviert, zeitlich begrenzt und
  in README sowie Benutzeroberfläche transparent dokumentiert sein.
- Bitte keine Telemetrie, Werbung oder extern geladenen Medien hinzufügen.
- Kommentare sollen Absicht und sicherheitsrelevante Entscheidungen erklären,
  nicht lediglich den Code wiederholen.

## Lizenz von Beiträgen

Mit dem Einreichen eines Beitrags erklärst du dich damit einverstanden, dass
dein Beitrag unter der [MIT-Lizenz](LICENSE) des Projekts veröffentlicht wird.

## Datenschutz bei Fehlerberichten

Logs können IP-Adressen, Hostnamen, Benutzerpfade oder Prozessnamen enthalten.
Vor dem Anhängen an ein öffentliches Issue müssen solche Angaben entfernt oder
unkenntlich gemacht werden. Sicherheitslücken bitte gemäß `SECURITY.md`
vertraulich melden.
