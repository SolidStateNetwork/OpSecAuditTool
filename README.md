<p align="center">
  <img src="Assets/AppIcon.png" width="150" alt="SolidStateNetwork App-Icon">
</p>

<h1 align="center">OpSec Audit Tool</h1>

<p align="center">
  Portable OpSec-, Datenschutz-, Forensik- und Systemhärtungsprüfung für Linux
  und Windows.
</p>

<p align="center">
  <a href="https://github.com/SolidStateNetwork/OpSecAuditTool/releases/latest">
    <img src="https://img.shields.io/github/v/release/SolidStateNetwork/OpSecAuditTool?style=flat-square&amp;color=00ff66" alt="Aktueller Release">
  </a>
  <a href="https://github.com/SolidStateNetwork/OpSecAuditTool/actions/workflows/build.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/SolidStateNetwork/OpSecAuditTool/build.yml?branch=main&amp;style=flat-square&amp;label=build" alt="Build-Status">
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/SolidStateNetwork/OpSecAuditTool?style=flat-square" alt="MIT-Lizenz">
  </a>
</p>

> [!IMPORTANT]
> Die Anwendung liefert technische Hinweise, ersetzt aber keine professionelle
> Sicherheitsbewertung. Heuristische und unvollständig prüfbare Ergebnisse werden
> bewusst nicht als bestätigte Sicherheit dargestellt.

## Download

Die aktuellen selbstenthaltenen Pakete für **Linux x64** und **Windows x64**
stehen mit SHA-256-Prüfsummen im
[neuesten GitHub-Release](https://github.com/SolidStateNetwork/OpSecAuditTool/releases/latest)
bereit. Auf dem Zielsystem ist keine separate .NET-Installation erforderlich.

## Screenshots

### Lokales OpSec-Kontrollzentrum

![Aktuelle Übersicht im Cyber-Terminal-Design mit animiertem Radar und lokalem OpSec-Kontrollzentrum](docs/images/overview-cyber-terminal.png)

### System- und Sicherheits-Audit

![Vollständig ausgeführter und ausgeklappter System- und Sicherheits-Audit mit Bewertung, Signalfarben und mehreren Reihen kompakter Ergebniskarten](docs/images/audit-expanded-results.png)

## Eigenschaften

- lokale, ausschließlich lesende Sicherheits- und Systemprüfungen
- Betrieb als normaler Benutzer ohne angeforderte Administrator- oder Root-Rechte
- portable, selbstenthaltene Builds für Linux x64 und Windows x64
- nachvollziehbare Pass-, Warnungs- und Fehlerzustände
- kompakte, aufklappbare Ergebniskarten und exportierbare Textberichte
- Netzwerkzugriffe standardmäßig deaktiviert
- keine Werbung und keine Analyse-Telemetrie
- lokale Protokolle und Einstellungen im portablen Programmordner

## Projektstruktur

- `Core/` enthält die fachlichen Prüfungen. Jede implementiert `IOpSecChecker`
  und liefert genau ein `CheckResult`.
- `Core/AuditCheckerCatalog.cs` stellt die gemeinsamen und
  plattformspezifischen Prüfungen zusammen.
- `Services/` kapselt Einstellungen, Logging, Berichte, Systeminformationen,
  Prozessabfragen und optionale Netzwerkzugriffe.
- `Models/`, `ViewModels/` und `Views/` bilden die Avalonia-Oberfläche.

## Bewertungsmodell

- `Pass`: Die konkret gemessene Eigenschaft wurde erfolgreich geprüft und ist im
  Rahmen dieser Prüfung unauffällig.
- `Warning`: Aufmerksamkeit erforderlich, Ergebnis heuristisch oder Zustand nicht
  vollständig verifizierbar.
- `Fail`: Kritisches Ergebnis oder eine ausdrücklich erforderliche Prüfung wurde
  nicht ausgeführt.

Der Prozentwert wird einfach berechnet:

```text
erfolgreiche Prüfungen / alle Prüfungen × 100
```

Warnungen, Fehler und übersprungene Prüfungen erhöhen den Prozentwert nicht.
Eine Prüfung darf niemals allein wegen fehlender Leserechte oder einer nicht
vorhandenen Schnittstelle als bestanden gelten.

## Netzwerkzugriffe

Netzwerkzugriffe sind standardmäßig deaktiviert. Nach ausdrücklicher Aktivierung
fragt ausschließlich die Prüfung der öffentlichen IP `https://api.ipify.org` ab
und verwendet `https://icanhazip.com` als Fallback. Die DNS-Prüfung wertet nur die
lokal sichtbare Resolver-Konfiguration aus und stellt keine Internetverbindung
her. Es werden keine Werbe-, Medien- oder Telemetriedienste kontaktiert.

## Datenschutz bei Logs und Berichten

Konkrete öffentliche IP-Adressen sowie Host- und Benutzernamen werden nicht in
das dauerhafte Anwendungslog geschrieben. Exportierte Berichte redigieren IP-
und MAC-Adressen automatisch und lassen Hostname, lokale IP und MAC-Adresse aus.

Ergebnisdetails können dennoch lokale Pfade, Prozessnamen oder Konfigurationsnamen
enthalten. Prüfe Logs und Reports deshalb vor einer Veröffentlichung.

## Bauen

Voraussetzung ist ein .NET SDK, das `net10.0` unterstützt.

```bash
dotnet restore
dotnet format OpSecAuditTool.csproj --verify-no-changes
dotnet build OpSecAuditTool.csproj --configuration Release
```

Portable Builds:

```bash
dotnet publish -p:PublishProfile=WindowsPortable
dotnet publish -p:PublishProfile=LinuxPortable
```

Die Ergebnisse landen unter `artifacts/`. Weitere Hinweise stehen in
[LINUX_PORTABLE.md](LINUX_PORTABLE.md) und
[WINDOWS_PORTABLE.md](WINDOWS_PORTABLE.md).

## Neue Prüfung hinzufügen

1. Eine Klasse unter der passenden `Core/`-Kategorie anlegen.
2. `IOpSecChecker` implementieren.
3. Unbekannte, übersprungene oder unvollständig prüfbare Zustände nicht als
   `Pass` zurückgeben.
4. Die Prüfung in `AuditCheckerCatalog` registrieren.
5. Formatierung, Release-Build und beide Plattformprofile prüfen.

Details zu Beiträgen stehen in [CONTRIBUTING.md](CONTRIBUTING.md).
Sicherheitsprobleme bitte gemäß [SECURITY.md](SECURITY.md) vertraulich melden.

## Lizenz

Der eigene Quellcode steht unter der [MIT-Lizenz](LICENSE),
Copyright © 2026 SolidStateNetwork. Eingebundene Bibliotheken und Assets behalten
ihre jeweiligen Lizenzbedingungen.
