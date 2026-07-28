<p align="center">
  <img src="Assets/AppIcon.png" width="100" alt="OpSec Audit Tool">
</p>

<h1 align="center">OpSec Audit Tool</h1>

<p align="center">
  Portable Sicherheitsanalyse für Linux und Windows.<br>
  <b>Keine Installation. Keine Admin-Rechte. Keine Telemetrie.</b>
</p>

<p align="center">
  <a href="https://github.com/SolidStateNetwork/OpSecAuditTool/releases/latest">
    <img src="https://img.shields.io/github/v/release/SolidStateNetwork/OpSecAuditTool?style=flat-square&color=00ff66&label=release" alt="Release">
  </a>
  <a href="https://github.com/SolidStateNetwork/OpSecAuditTool/actions/workflows/build.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/SolidStateNetwork/OpSecAuditTool/build.yml?branch=main&style=flat-square&label=build" alt="Build">
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/SolidStateNetwork/OpSecAuditTool?style=flat-square&color=555" alt="MIT">
  </a>
</p>

---

Das OpSec Audit Tool prüft dein System auf Härtungslücken, forensische Rückstände und Privacy-Leaks. 67 spezialisierte Checker laufen vollständig im User-Space – ohne Root-Rechte, ohne Netzwerkzugriff und ohne Daten an Dritte zu senden.

Das Ergebnis ist ein nachvollziehbarer **Security Score** mit farbcodierten Befunden, technischen Details und automatisierten **Quick-Fixes** für häufige Schwachstellen.

<p align="center">
  <img src="docs/images/overview-cyber-terminal.png" width="820" alt="OpSec Audit Tool – Kontrollzentrum">
</p>

<p align="center">
  <img src="docs/images/audit-expanded-results.png" width="820" alt="OpSec Audit Tool – Audit-Ergebnisse">
</p>

---

## Funktionsumfang

**Systemhärtung** — SSH-Server- und Client-Konfiguration, Sudoers-Analyse, Disk-Encryption, Firewall-Status, Kernel-Flags (ASLR, ptrace, Lockdown), Secure Boot, Swap-Verschlüsselung und USB-Guard.

**Netzwerk & Privacy** — Port-Scan aller 65.536 Ports über Kernel-Socket-Tabellen mit Bindungsanalyse (lokal vs. öffentlich), Tor-Konfigurationserkennung, DNS-Leak-Tests, VPN-Interface-Prüfung und MAC-Randomisierung.

**Forensik & Anti-Forensik** — Browser-Profile und -Erweiterungen (Native, Flatpak, Snap), Messenger-Storage, WebRTC-Leak-Erkennung, Crash-Dumps, Clipboard-Inhalte, Thumbnail-Caches, Papierkorb und tmpfs-Status.

**Diagnostik** — Umgebungsvariablen auf versehentlich exportierte API-Tokens (OpenAI, AWS, GCP), Shell-History auf Klartext-Passwörter, AI-Tooling-Konfigurationen, Telemetrie-Dienste und Systemd-Journal.

**Container & Credentials** — Docker/Podman-Gruppenrechte, Git-Credential-Store, GPG-Schlüsselqualität, FIDO2/U2F-Hardware-Tokens, SSH/GPG-Agent-Socket-Konfiguration und Shell-Startup-Injektionen.

**Windows** — Defender-Status, BitLocker, Credential Guard, Firewall-Profile, Datenschutzeinstellungen, RDP-Konfiguration und Autostart-Analyse.

<details>
<summary><b>Vollständige Liste aller 67 Checker anzeigen</b></summary>
<br>

#### Security & Härtung (23)

| Checker | Prüfbereich |
|:--|:--|
| `OpenPortsChecker` | 65.536-Port-Scan via Kernel-Socket-Tabellen |
| `SshHardeningChecker` | sshd_config (Root-Login, Passwort-Auth) |
| `SshClientConfigChecker` | SSH-Client-Flags (HostKeyChecking, ForwardAgent) |
| `SshKnownHostsHygieneChecker` | Klartext-Hostnamen in known_hosts |
| `GpgKeySecurityChecker` | Schlüssellängen & abgelaufene Keys |
| `HardwareAuthTokenChecker` | FIDO2/U2F am USB-Bus |
| `DockerPodmanSecurityChecker` | Container-Gruppenrechte & Registry-Tokens |
| `GitSecurityConfigChecker` | git-credentials & store-Helper |
| `SudoersChecker` | NOPASSWD / !authenticate |
| `ShellStartupPersistenceChecker` | Injektionen in Shell-Profile |
| `UserAutostartChecker` | Autostart & User-systemd Units |
| `AgentSocketSecurityChecker` | SSH/GPG-Agent Socket-Konfiguration |
| `DiskEncryptionChecker` | LUKS, eCryptfs, dm-crypt |
| `FirewallChecker` | UFW, firewalld, nftables |
| `SwapMemoryChecker` | Verschlüsselter Swap / ZRAM |
| `UsbGuardChecker` | BadUSB-Schutz |
| `UserDataPermissionsChecker` | Rechte auf ~/.ssh, ~/.gnupg |
| `MacSpoofChecker` | MAC-Randomisierung |
| `DisplayServerSecurityChecker` | Wayland vs. X11 |
| `PackageManagerSecurityChecker` | pip/npm/yarn Härtung |
| `SandboxPermissionChecker` | Flatpak/Snap Berechtigungen |
| `ScreenLockTimeoutChecker` | Bildschirmsperre |
| `PtraceScopeChecker` | Kernel ptrace Scope |

#### Forensik & Anti-Forensik (10)

| Checker | Prüfbereich |
|:--|:--|
| `WebRtcLeakChecker` | WebRTC STUN IP-Leaks in Browser-Profilen |
| `MessengerStoragePrivacyChecker` | Signal, Telegram, Discord, Element |
| `LocalCrashDumpFileChecker` | .core/.dmp-Dateien mit RAM-Auszügen |
| `BrowserStorageChecker` | Chrome, Brave, Edge, Firefox |
| `BrowserExtensionAuditChecker` | Extension-Count & Berechtigungen |
| `TrashChecker` | Papierkorb & .Trash-1000 |
| `ClipboardChecker` | Private Keys in der Zwischenablage |
| `TmpFsChecker` | /tmp als RAM-Dateisystem |
| `RecentFilesChecker` | Dokumentenverlauf |
| `ThumbnailCacheChecker` | Thumbnail-Caches |

#### Diagnostik & Hygiene (8)

| Checker | Prüfbereich |
|:--|:--|
| `EnvironmentSecretChecker` | API-Tokens in Umgebungsvariablen |
| `AiToolingPrivacyChecker` | Klartext-Tokens in AI-Tools |
| `ShellHistoryChecker` | Passwörter in Shell-History |
| `CrashReportChecker` | coredumpctl & WER-Verzeichnisse |
| `TelemetryChecker` | Aktive Telemetrie-Dienste |
| `CronJobChecker` | Verdächtige Cron-Einträge |
| `FailedServicesChecker` | Fehlgeschlagene systemd Units |
| `JournaldChecker` | Persistente Journal-Logs |

#### Netzwerk & Privacy (9)

| Checker | Prüfbereich |
|:--|:--|
| `TorStatusChecker` | Tor-Daemon & torrc-Konfiguration |
| `EncryptedDnsChecker` | DNS-over-TLS / DNS-over-HTTPS |
| `DnsLeakChecker` | DNS-Leak-Erkennung |
| `IpPublicChecker` | Öffentliche IP & Geolocation |
| `LocalHostsFileChecker` | /etc/hosts Analyse |
| `BluetoothChecker` | Bluetooth-Status |
| `ExternalListenerChecker` | Extern erreichbare Dienste |
| `TorrentLeakChecker` | BitTorrent IP-Leaks |
| `WifiSecurityChecker` | WLAN-Verschlüsselung |

#### System & Kernel (7)

| Checker | Prüfbereich |
|:--|:--|
| `AslrChecker` | Address Space Layout Randomization |
| `CoreDumpChecker` | Core-Dump-Konfiguration |
| `KernelLockdownChecker` | Kernel Lockdown Mode |
| `KernelModuleChecker` | Geladene Kernel-Module |
| `SecureBootChecker` | Secure Boot Status |
| `DisplayServerChecker` | Display-Server Erkennung |
| `HostnameTimezoneChecker` | Hostname & Zeitzone |

#### Windows (10)

| Checker | Prüfbereich |
|:--|:--|
| `WindowsDefenderChecker` | Defender & Echtzeit-Schutz |
| `WindowsFirewallChecker` | Firewall-Profile |
| `WindowsBitLockerChecker` | BitLocker-Verschlüsselung |
| `WindowsSecureBootChecker` | UEFI Secure Boot |
| `WindowsPrivacyChecker` | Telemetrie & Datenschutz |
| `WindowsAccountProtectionChecker` | Kontoschutz |
| `WindowsCredentialProtectionChecker` | Credential Guard |
| `WindowsRemoteAccessChecker` | RDP & Remote-Zugriff |
| `WindowsStartupPersistenceChecker` | Autostart-Einträge |
| `WindowsWirelessChecker` | WLAN-Sicherheit |

</details>

---

## Schnellstart

**Portables Release** — Self-Contained, keine Installation nötig:

```bash
tar -xzf OpSecAuditTool-v1.1.0-linux-x64.tar.gz
./OpSecAuditTool
```

**Aus dem Source** — benötigt .NET 10 SDK:

```bash
git clone https://github.com/SolidStateNetwork/OpSecAuditTool.git
cd OpSecAuditTool
dotnet run
```

Downloads und SHA-256-Prüfsummen unter [Releases](https://github.com/SolidStateNetwork/OpSecAuditTool/releases/latest).

---

## Designprinzipien

- **User-Space only** — Keine Root-Rechte, kein sudo, keine UAC-Elevation. Alle Abfragen sind fehlersicher gekapselt.
- **Offline by default** — Kein Nachladen externer Ressourcen. Online-Prüfungen (DNS-Leak, Tor-Exit) erfordern explizite Freigabe in den Einstellungen.
- **Transparenz** — Jeder Befund zeigt die geprüfte Konfiguration, den konkreten Befundtext und ggf. einen Quick-Fix.
- **Portabilität** — Einstellungen, Logs und Berichte liegen neben der Anwendung. Kein Schreibzugriff außerhalb des eigenen Verzeichnisses.

---

## Lizenz & Mitwirken

Veröffentlicht unter der [MIT-Lizenz](LICENSE). Beiträge sind willkommen — siehe [CONTRIBUTING.md](CONTRIBUTING.md).  
Sicherheitslücken bitte vertraulich melden — siehe [SECURITY.md](SECURITY.md).
