# Portabler Windows-Build

Der Windows-Build arbeitet als normaler Benutzer und fordert gemäß Manifest
keine UAC-Erhöhung an. Alle veränderlichen Daten bleiben im Programmordner:

- `Settings/`
- `Logs/`
- `Reports/`

Der Ordner muss deshalb beschreibbar sein. Kopiere die Anwendung beispielsweise
in einen eigenen Ordner, auf einen USB-Datenträger oder in das Benutzerprofil,
nicht in `C:\Program Files`.

## Build

```bash
dotnet publish -p:PublishProfile=WindowsPortable
```

Das Ergebnis liegt unter `artifacts/windows-portable/` und enthält die benötigte
.NET-Laufzeit. Der gesamte Ordner kann auf ein Windows-x64-System kopiert werden.

## Verhalten ohne Administratorrechte

Die Audit-Checks verändern keine Systemeinstellungen. Wenn Windows einen Status
für Standardbenutzer nicht freigibt, wird das Ergebnis als Warnung und nicht als
„bestanden“ gewertet. Die Anwendung startet keine automatische UAC-Abfrage.
