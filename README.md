<p align="center">
  <img src="Assets/AppIcon.png" width="150" alt="SolidStateNetwork App-Icon">
</p>

<h1 align="center">OpSec Audit Tool</h1>

<p align="center">
  <b>Enterprise-Grade, Zero-Admin OpSec-, Forensik- & Systemhärtungs-Audit für Linux und Windows</b><br>
  <i>„OpSec ist keine magische Liste von Regeln, die man blind befolgt – sondern kontinuierliche Verifikation.“</i>
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
> **Zero-Admin Garantie**: Dieses Tool arbeitet vollständig im **User-Space** als normaler Standardbenutzer – ohne `sudo`, ohne `root`-Rechte und ohne UAC-Erhöhung. Alle Abfragen sind ausfallsicher gekapselt und liefern bei geschützten Systembereichen saubere Hinweise statt Fehlalarmen.

---

## 🌟 Highlight-Features

- **🛡️ 100 % User-Space & Sudo-frei**: Führt tiefe Analysen (SSH, Sudoers, Kernel-Flags, Berechtigungen, Coredumps) im normalen Benutzerkontext aus, ohne Root-Rechte anzufordern oder bei Berechtigungsbarrieren abzustürzen.
- **🌐 Universal-Port-Scanner (0–65535)**: Dynamisches Auslesen von Kernel-Socket-Tabellen (`ss -tln` / `/proc/net/tcp`) zur lückenlosen Erkennung sämtlicher offener Ports mit Unterscheidung zwischen **öffentlich exponiert (`0.0.0.0` / `[::]`)** und **lokal gebunden (`127.0.0.1` / `[::1]`)** inkl. AI-/DB-Dienste (`Ollama`, `Redis`, `PostgreSQL`, `MongoDB` etc.).
- **🧅 Dynamischer Tor- & Privacy-Audit**: Liest lokale `torrc`-Dateien (`/etc/tor/torrc`, `~/.tor/torrc`, Flatpak/Snap) dynamisch ein, erkennt benutzerdefinierte `SocksPort`-, `ControlPort`- oder `HTTPTunnelPort`-Konfigurationen und verifiziert den aktiven Tor-Daemon.
- **⚡ Parallele Audit-Engine**: 68 spezialisierte Core-Checker werden asynchron und ressourcenschonend im Hintergrund ausgeführt (`SemaphoreSlim`), ohne das UI zu blockieren.
- **⚡ Automatische Sofort-Härtung (Quick-Fixes)**: Ausgewählte Checker bieten per Knopfdruck direkte Automations-Fixes im User-Space (z. B. Deaktivierung von WebRTC in Firefox `user.js`, Hashen von SSH `known_hosts`, Härten von `pip`/`npm`, Löschen des Dokumentenverlaufs).
- **🎨 Cyber-Terminal Design**: Kontrastreiches, reaktives Neon-Cyber-Terminal-Theme für Avalonia UI mit dynamischem Live-Radar, ausklappbaren Ergebniskarten und Echtzeit-Verbindungsstatus.
- **📄 Multi-Format Reporting**: Lokaler Export von vollständigen Audit-Berichten als **Markdown**, **JSON**, **HTML** und **PDF**.
- **🔒 Zero Telemetry & Offline-First**: Kein Nachladen externer Scripte, keine Tracker, vollständiger Offline-Betrieb. Online-Prüfungen (z. B. DNS-Leak oder Tor-Exit-Check) müssen in den Einstellungen explizit vom Nutzer freigegeben werden.

---

## 📥 Download & Portabilität

| Plattform | Portables Paket | Beschreibung |
|---|---|---|
| **Linux x64** | [OpSecAuditTool-v1.1.0-linux-x64.tar.gz](https://github.com/SolidStateNetwork/OpSecAuditTool/releases/download/v1.1.0/OpSecAuditTool-v1.1.0-linux-x64.tar.gz) | Selbstenthaltenes Paket inkl. .NET Runtime |
| **Windows x64** | [OpSecAuditTool-v1.1.0-windows-x64.zip](https://github.com/SolidStateNetwork/OpSecAuditTool/releases/download/v1.1.0/OpSecAuditTool-v1.1.0-windows-x64.zip) | Selbstenthaltenes Paket inkl. .NET Runtime |

Beide Pakete sind **vollständig portabel** (Self-Contained Single-File Option verfügbar) und erfordern **keine separate .NET-Installation**. SHA-256-Prüfsummen stehen beim [aktuellen Release](https://github.com/SolidStateNetwork/OpSecAuditTool/releases/latest) bereit.

---

## 🔍 Übersicht der 68 Core-Checker

Das Tool teilt seine Prüfungen in sechs modulare Domänen auf. Jeder Checker erbt von der robusten Basisklasse `OpSecCheckerBase`:

### 1. Security / Härtung (`Core/Security/`)
| Checker | Prüfziel |
| :--- | :--- |
| **`OpenPortsChecker`** | Universal-Scan aller 65.536 Ports via Kernel-Socket-Tabellen (`ss` / `/proc/net/tcp`) |
| **`SshHardeningChecker`** | Prüfung von `/etc/ssh/sshd_config` & `/etc/ssh/sshd_config.d/*.conf` (Root-Login, Passwort-Auth) |
| **`SshClientConfigChecker`** | Prüfung des SSH-Clients (`~/.ssh/config`) auf riskante HostKeyChecking- & ForwardAgent-Flags |
| **`SshKnownHostsHygieneChecker`** | Prüfung von `~/.ssh/known_hosts` auf im Klartext gespeicherte Hostnamen und IP-Adressen |
| **`GpgKeySecurityChecker`** | Prüfung des GPG-Schlüsselrings im User-Space auf schwache Schlüssellängen (< 2048 Bit) & Expired Keys |
| **`HardwareAuthTokenChecker`** | Erkennung von FIDO2 / U2F Hardware-Tokens (YubiKey, Nitrokey, SoloKeys) am USB-Bus |
| **`SudoersChecker`** | Erkennung von `NOPASSWD` / `!authenticate` in `/etc/sudoers` & `/etc/sudoers.d/` |
| **`DockerPodmanSecurityChecker`** | Container-Sicherheit (`docker`-Gruppenrechte & Klartext-Registry-Tokens) |
| **`GitSecurityConfigChecker`** | Prüfung auf unverschlüsselte `~/.git-credentials` und riskanten `store`-Helper |
| **`ShellStartupPersistenceChecker`** | Analyse von Shell-Profilen (`~/.bashrc` etc.) auf Injektionen (`LD_PRELOAD`, alias-Überschreibungen) |
| **`UserAutostartChecker`** | User-Space Autostart-Audit (`~/.config/autostart` & User-systemd Units) |
| **`AgentSocketSecurityChecker`** | SSH- & GPG-Agent Socket-Prüfung auf unbegrenzt im Speicher gehaltene Schlüssel |
| **`DiskEncryptionChecker`** | Prüfung auf aktive LUKS-, eCryptfs- oder dm-crypt Systemverschlüsselung |
| **`FirewallChecker`** | Erkennung aktiver Linux-Firewalls (`UFW`, `firewalld`, `nftables`) |
| **`SwapMemoryChecker`** | Verifikation verschlüsselter Swap-Speicher / ZRAM |
| **`UsbGuardChecker`** | Schutz vor BadUSB / nicht autorisierten USB-Geräten (`usbguard`) |
| **`UserDataPermissionsChecker`** | Rechteprüfung sensibler Benutzerordner (`~/.ssh`, `~/.gnupg`, Browser-Profile) |
| **`MacSpoofChecker`** | Prüfung auf aktivierte MAC-Adressen-Anonymisierung im NetworkManager |

### 2. Forensik & Anti-Forensik (`Core/Forensics/`)
| Checker | Prüfziel |
| :--- | :--- |
| **`WebRtcLeakChecker`** | Browser-Profil-Audit (Firefox, Chrome, Brave) auf WebRTC STUN IP-Leaks |
| **`MessengerStoragePrivacyChecker`** | Prüfung lokaler Profilordner von Desktop-Messengern (Signal, Telegram, Discord, Element) |
| **`LocalCrashDumpFileChecker`** | Scannt User-Verzeichnisse auf verwaiste `.core`- und `.dmp`-Dateien mit RAM-Auszügen |
| **`BrowserStorageChecker`** | Analyse von Browser-Profilen (Native, **Flatpak**, **Snap** für Chrome, Brave, Edge, Firefox) |
| **`BrowserExtensionAuditChecker`** | Zählt und prüft installierte Browser-Erweiterungen und Extension-Berechtigungen |
| **`TrashChecker`** | Safe-rekursiver Scan auf Datenreste im Papierkorb & externen `.Trash-1000` Verzeichnissen |
| **`ClipboardChecker`** | Überwachung der Zwischenablage auf unverschlüsselte Private Keys oder Tokens |
| **`TmpFsChecker`** | Prüfung, ob `/tmp` und `/var/tmp` als flüchtiges RAM-Dateisystem (`tmpfs`) gebunden sind |
| **`RecentFilesChecker`** | Analyse und Warnung bei aktiviertem System-Verlauf zuletzt geöffneter Dokumente |
| **`ThumbnailCacheChecker`** | Erkennung von forensisch auswertbaren Thumbnail-Caches in `~/.cache/thumbnails` |
| **`SystemLogScrubberChecker`** | Prüfung auf rotierende, dauerhaft gespeicherte Journald-/System-Logs |

### 3. Diagnostik & Hygiene (`Core/Diagnostics/`)
| Checker | Prüfziel |
| :--- | :--- |
| **`EnvironmentSecretChecker`** | Spürt versehentlich exportierte AI-/Cloud-API-Tokens (`OPENAI_API_KEY`, `GEMINI_API_KEY`, AWS, GCP, JWT) auf |
| **`AiToolingPrivacyChecker`** | Analyse lokaler AI- / LLM-Tools (Continue, Aider, Copilot, Cursor) auf Klartext-Tokens |
| **`ShellHistoryChecker`** | Asynchrones Streaming der Shell-History auf im Klartext getippte Passwörter & Tokens (`max 15.000` Zeilen) |
| **`CrashReportChecker`** | Coredump-Index-Prüfung (`coredumpctl`) und WER-Verzeichnisse auf Arbeitsspeicher-Auszüge |
| **`FailedServicesChecker`** | Erkennung fehlerhafter `systemd`-Dienste im Status `failed` |
| **`TelemetryChecker`** | Verifikation deaktivierter System- und CLI-Telemetrie (`DOTNET_CLI_TELEMETRY_OPTOUT`, etc.) |

### 4. Netzwerk & Anonymität (`Core/Network/`)
| Checker | Prüfziel |
| :--- | :--- |
| **`TorStatusChecker`** | Dynamischer Parser für `torrc`-Ports + Erkennung von Tor SOCKS5, ControlPort und TransPort |
| **`EncryptedDnsChecker`** | Prüfung auf verschlüsseltes DNS-over-TLS (`DNSOverTLS=yes`) oder lokale DNS-Proxys |
| **`LocalHostsFileChecker`** | Prüfung der `/etc/hosts` Datei auf Manipulationen und DNS-Hijacking von Sicherheitsdomains |
| **`DnsLeakChecker`** | Prüfung der DNS-Auflösung und Schutz vor unverschlüsseltem DNS-Leak |
| **`IpPublicChecker`** | Lokale oder optionale Online-Erkennung der öffentlich sichtbaren IP-Adresse |
| **`TorrentLeakChecker`** | Schutz vor IP-Leaks durch P2P-/Torrent-Dienste ohne aktiven VPN-Tunnel |
| **`WifiSecurityChecker`** | Bewertung von WPA2/WPA3 WLAN-Sicherheit und Schutz vor unverschlüsselten Netzen |
| **`ExternalListenerChecker`** | Warnung bei Prozessen, die auf extern erreichbaren Netzwerk-Interfaces lauschen |
| **`BluetoothChecker`** | Sicherheitsprüfung des lokalen Bluetooth-Stacks |

### 5. System & Hardware (`Core/System/`)
| Checker | Prüfziel |
| :--- | :--- |
| **`SandboxPermissionChecker`** | Prüfung von Flatpak & Snap Overrides auf riskante `--filesystem=home` / `--filesystem=host` Rechte |
| **`PackageManagerSecurityChecker`** | Audit von `~/.config/pip/pip.conf` (`require-virtualenv`) und `~/.npmrc` (`ignore-scripts`) |
| **`ScreenLockTimeoutChecker`** | Verifikation aktiver automatischer Bildschirmsperren & Idle-Timeouts (< 15 Min.) |
| **`SecureBootChecker`** | Prüfung des UEFI SecureBoot-Status via `efivars` oder automatisch über `mokutil --sb-state` |
| **`DisplayServerSecurityChecker`** | Prüfung auf modernes Wayland vs. X11 (welches globales Keylogging erlaubt) |
| **`AslrChecker`** | Verifikation von Address Space Layout Randomization (`kernel.randomize_va_space = 2`) |
| **`KernelModuleChecker`** | Prüfung der Sicherheitseinstellungen von Kernel-Modulen |
| **`HostnameTimezoneChecker`** | Analyse auf forensisch oder zeitzonenbedingt identifizierende Hostnamen |

### 6. Windows-Spezifisch (`Core/Windows/`)
| Checker | Prüfziel |
| :--- | :--- |
| **`WindowsDefenderChecker`** | Prüfung des Microsoft Defender Echtzeitschutzes |
| **`WindowsBitLockerChecker`** | Verifikation der BitLocker-Laufwerksverschlüsselung |
| **`WindowsFirewallChecker`** | Prüfung der Windows-Defender-Firewall auf allen Profilen |
| **`WindowsAccountProtectionChecker`** | Analyse von Benutzerkontensteuerung (UAC) und Gastkonten |
| **`WindowsCredentialProtectionChecker`** | Verifikation von Credential Guard und LSASS-Schutz |
| **`WindowsWirelessChecker`** | WLAN-Sicherheitsbewertung unter Windows |

---

## 🏗️ Projektstruktur & Architektur

```text
├── Core/
│   ├── Security/         # Härtungs- und Ports-Prüfungen
│   ├── Forensics/        # Anti-Forensik & Browser-Analyse
│   ├── Diagnostics/      # Secrets, Logs & Absturzberichte
│   ├── Network/          # Tor, DNS, VPN & Netzwerk-Hygiene
│   ├── System/           # SecureBoot, ASLR & Kernel-Härtung
│   └── Windows/          # Windows-spezifische Sicherheitsprüfungen
├── Models/               # Darstellungsmodelle für UI & Reports
├── Services/             # Konfiguration, Shell-Befehle, Export & Logging
├── ViewModels/           # Modulare ViewModels (Main, AuditRunner, SystemDashboard, Contact)
├── Views/                # Avalonia XAML-Layouts (Cyber-Terminal Theme)
└── README.md             # Diese Dokumentation
```

### Clean Architecture & Separation of Concerns
- **`OpSecCheckerBase`**: Einheitliche abstrakte Basisklasse, die Exception-Handling, asynchrones Logging und standardisierte Ergebnis-Factories (`Pass()`, `Warning()`, `Fail()`, `Error()`) kapselt.
- **Modulare ViewModels**: Vollständige Entkopplung zwischen der Navigationsschicht (`MainViewModel`), der Ausführungs-Engine (`AuditRunnerViewModel`), der Live-Systemübersicht (`SystemDashboardViewModel`) und der Kontaktansicht (`ContactViewModel`).
- **`ShellCommandService`**: Sichere, asynchrone Prozessausführung im User-Space ohne Shell-Injection-Gefahren.

---

## 📊 Bewertungsmodell

Der Gesamtreife-Score visualisiert die Sicherheit in einem klaren Prozentwert:

$$\text{Score} = \frac{\text{Bestandene Prüfungen (\texttt{Pass})}}{\text{Ausgeführte und bewertbare Prüfungen}} \times 100$$

- **`Pass` (Grün)**: Bestanden, sicher konfiguriert.
- **`Warning` (Gelb)**: Funktional sicher, erfordert aber Aufmerksamkeit oder Optimierung.
- **`Fail` (Rot)**: Kritisches Sicherheitsrisiko oder unvollständige Härtung.
- **`Error` / Übersprungen**: Plattformspezifisch nicht zutreffend (z. B. Windows-Checker unter Linux) – beeinflusst den Nenner nicht negativ.

---

## 💻 Bauen & Ausführen aus dem Quellcode

### Voraussetzungen
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (oder .NET 8.0/9.0 je nach Umgebung)
- Linux x64 oder Windows x64

### Lokal starten (Entwicklung)
```bash
# Repository klonen
git clone git@github.com:SolidStateNetwork/OpSecAuditTool.git
cd OpSecAuditTool

# Abhängigkeiten wiederherstellen und Anwendung ausführen
dotnet run
```

### Portable Release-Binary kompilieren
```bash
# Für Linux x64 (Self-Contained)
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# Für Windows x64 (Self-Contained)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 🔐 Datenschutz & Kontakt

Dieses Tool sendet **keine Telemetriedaten** und stellt in der Standardeinstellung **keine Netzwerkverbindungen** her.  
Fragen, Vorschläge oder Security-Reports können direkt über die integrierte **Kontaktübersicht** im Tool (inklusive XMPP- und PGP-Schlüssel) oder über [GitHub Issues](https://github.com/SolidStateNetwork/OpSecAuditTool/issues) eingereicht werden.
