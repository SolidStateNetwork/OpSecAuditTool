# Portabler Linux-Build

Der Linux-Build läuft als normaler Benutzer und fordert keine Root-Rechte an.
Alle veränderlichen Daten bleiben direkt im Programmordner:

- `Settings/`
- `Logs/`
- `Reports/`

Der Programmordner muss deshalb für den aktuellen Benutzer beschreibbar sein.

## Build

```bash
dotnet publish -p:PublishProfile=LinuxPortable
```

Das Ergebnis liegt unter `artifacts/linux-portable/` und enthält die benötigte
.NET-Laufzeit. Der vollständige Ordner kann auf ein anderes Linux-x64-System
kopiert werden.

## Start

```bash
chmod +x OpSecAuditTool
./OpSecAuditTool
```

Die Anwendung selbst benötigt keine Root-Rechte. Einzelne Audit-Ergebnisse können
als Warnung erscheinen, wenn das Betriebssystem bestimmte Informationen für
Standardbenutzer nicht freigibt.

## Systemabhängigkeiten

Der Build bringt .NET und seine verwalteten Bibliotheken mit. Auf dem Zielsystem
müssen weiterhin eine grafische Linux-Sitzung sowie die üblichen nativen
Desktopbibliotheken für Avalonia/Skia vorhanden sein.
