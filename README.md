<p align="center">
  <img src="Assets/AppIcon.png" width="110" alt="OpSec Audit Tool">
</p>

<h1 align="center">OpSec Audit Tool</h1>

<p align="center">
  <code>Zero-Admin</code> · <code>Zero-Telemetry</code> · <code>Offline-First</code><br>
  <b>Portable Systemhärtungs- & Forensik-Engine für Linux und Windows.</b>
</p>

<p align="center">
  <a href="https://github.com/SolidStateNetwork/OpSecAuditTool/releases/latest">
    <img src="https://img.shields.io/github/v/release/SolidStateNetwork/OpSecAuditTool?style=flat-square&color=00ff66&label=release" alt="Release">
  </a>
  <a href="https://github.com/SolidStateNetwork/OpSecAuditTool/actions/workflows/build.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/SolidStateNetwork/OpSecAuditTool/build.yml?branch=main&style=flat-square&label=build" alt="Build">
  </a>
  <img src="https://img.shields.io/badge/checkers-67-00ff66?style=flat-square" alt="67 Checker">
  <img src="https://img.shields.io/badge/telemetry-none-00ff66?style=flat-square" alt="Zero Telemetry">
  <img src="https://img.shields.io/badge/sudo-not%20required-00ff66?style=flat-square" alt="No Sudo">
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/SolidStateNetwork/OpSecAuditTool?style=flat-square&color=555" alt="MIT">
  </a>
</p>

<br>

<p align="center">
  <img src="docs/images/overview-cyber-terminal.png" width="820" alt="Cyber-Terminal Kontrollzentrum mit Live-Radar und OpSec-Statusanzeige">
</p>

<p align="center">
  <img src="docs/images/audit-expanded-results.png" width="820" alt="Audit-Ergebnisse mit Security Score, Signalfarben und Quick-Fix Buttons">
</p>

---

## Was macht das Ding?

67 spezialisierte Checker scannen dein System nach Schwachstellen, forensischen Rückständen und Privacy-Leaks – **vollständig im User-Space**, ohne Root-Rechte, ohne Installation, ohne nach Hause zu telefonieren.

Du bekommst einen **Security Score**, farbcodierte Ergebnisse mit technischen Details und für viele Befunde **1-Klick Quick-Fixes**, die das Problem direkt im User-Space beheben.

### Das Wichtigste auf einen Blick

| | |
|:--|:--|
| 🛡️ **Sudo-frei** | Tiefenanalyse von SSH, Sudoers, Kernel-Flags, Container-Rechten, GPG-Schlüsseln – alles ohne `root` |
| 🌐 **Port-Scanner 0–65535** | Kernel-Socket-Tabellen (`ss` / `/proc/net/tcp`), Unterscheidung lokal vs. öffentlich exponiert |
| 🧅 **Tor & Privacy** | Dynamische `torrc`-Erkennung (Native, Flatpak, Snap), SocksPort/ControlPort-Verifikation |
| ⚡ **Quick-Fixes** | SSH `known_hosts` hashen, WebRTC-Leaks in Firefox stoppen, `pip`/`npm` härten, AI-Tokens scrubben |
| 🔬 **Anti-Forensik** | Browser-Profile, Messenger-Storage, Crash-Dumps, Clipboard, Thumbnails, Trash, tmpfs |
| 🔒 **Offline-First** | Kein Nachladen, keine Tracker, keine Cloud. Online-Checks nur nach expliziter Freigabe |

---

## Die 67 Checker

Organisiert in sechs Domänen, jeder Checker erbt von `OpSecCheckerBase`:

<details>
<summary><b>🛡️ Security & Härtung</b> — 23 Checker</summary>

| Checker | Was wird geprüft |
|:--|:--|
| `OpenPortsChecker` | Vollständiger 65.536-Port-Scan via Kernel-Socket-Tabellen |
| `SshHardeningChecker` | `sshd_config` & `sshd_config.d/*.conf` (Root-Login, Passwort-Auth) |
| `SshClientConfigChecker` | `~/.ssh/config` auf riskante Flags (HostKeyChecking, ForwardAgent) |
| `SshKnownHostsHygieneChecker` | Klartext-Hostnamen in `known_hosts` |
| `GpgKeySecurityChecker` | Schwache Schlüssellängen (<2048 Bit) & abgelaufene Keys |
| `HardwareAuthTokenChecker` | FIDO2/U2F Tokens am USB-Bus (YubiKey, Nitrokey, SoloKeys) |
| `DockerPodmanSecurityChecker` | `docker`-Gruppenrechte & Klartext-Registry-Tokens |
| `GitSecurityConfigChecker` | Unverschlüsselte `~/.git-credentials` & `store`-Helper |
| `SudoersChecker` | `NOPASSWD` / `!authenticate` in Sudoers |
| `ShellStartupPersistenceChecker` | Injektionen in `~/.bashrc` (`LD_PRELOAD`, Alias-Overwrites) |
| `UserAutostartChecker` | `~/.config/autostart` & User-systemd Units |
| `AgentSocketSecurityChecker` | SSH/GPG-Agent Sockets mit unbegrenzt gecachten Keys |
| `DiskEncryptionChecker` | LUKS, eCryptfs, dm-crypt |
| `FirewallChecker` | UFW, firewalld, nftables |
| `SwapMemoryChecker` | Verschlüsselter Swap / ZRAM |
| `UsbGuardChecker` | BadUSB-Schutz via usbguard |
| `UserDataPermissionsChecker` | Rechte auf `~/.ssh`, `~/.gnupg`, Browser-Profile |
| `MacSpoofChecker` | MAC-Randomisierung im NetworkManager |
| `DisplayServerSecurityChecker` | Wayland vs. X11 Sicherheitsbewertung |
| `PackageManagerSecurityChecker` | pip/npm/yarn Härtung & Audit |
| `SandboxPermissionChecker` | Flatpak/Snap Sandbox-Berechtigungen |
| `ScreenLockTimeoutChecker` | Bildschirmsperre-Timeout |
| `PtraceScopeChecker` | Kernel ptrace Scope |

</details>

<details>
<summary><b>🔬 Forensik & Anti-Forensik</b> — 10 Checker</summary>

| Checker | Was wird geprüft |
|:--|:--|
| `WebRtcLeakChecker` | Browser-Profile auf WebRTC STUN IP-Leaks |
| `MessengerStoragePrivacyChecker` | Lokale Profile von Signal, Telegram, Discord, Element |
| `LocalCrashDumpFileChecker` | Verwaiste `.core`/`.dmp`-Dateien mit RAM-Auszügen |
| `BrowserStorageChecker` | Chrome, Brave, Edge, Firefox (Native, Flatpak, Snap) |
| `BrowserExtensionAuditChecker` | Extension-Count & Berechtigungen |
| `TrashChecker` | Papierkorb & externe `.Trash-1000` Verzeichnisse |
| `ClipboardChecker` | Private Keys oder Tokens in der Zwischenablage |
| `TmpFsChecker` | `/tmp` und `/var/tmp` als RAM-Dateisystem |
| `RecentFilesChecker` | System-Verlauf zuletzt geöffneter Dokumente |
| `ThumbnailCacheChecker` | Forensisch auswertbare Thumbnail-Caches |

</details>

<details>
<summary><b>🧪 Diagnostik & Hygiene</b> — 8 Checker</summary>

| Checker | Was wird geprüft |
|:--|:--|
| `EnvironmentSecretChecker` | `OPENAI_API_KEY`, AWS, GCP, JWT in Umgebungsvariablen |
| `AiToolingPrivacyChecker` | Klartext-Tokens in Aider, Cursor, Copilot, Continue |
| `ShellHistoryChecker` | Klartext-Passwörter in bis zu 15.000 Zeilen History |
| `CrashReportChecker` | `coredumpctl`-Index & WER-Verzeichnisse |
| `TelemetryChecker` | Aktive Telemetrie-Dienste & Opt-Out-Status |
| `CronJobChecker` | Verdächtige Cron-Einträge |
| `FailedServicesChecker` | Fehlgeschlagene systemd Units |
| `JournaldChecker` | Persistente Journal-Logs & Rotation |

</details>

<details>
<summary><b>🌐 Netzwerk & Privacy</b> — 9 Checker</summary>

| Checker | Was wird geprüft |
|:--|:--|
| `TorStatusChecker` | Tor-Daemon & dynamische `torrc`-Konfiguration |
| `EncryptedDnsChecker` | DNS-over-TLS / DNS-over-HTTPS |
| `DnsLeakChecker` | DNS-Leak-Erkennung |
| `IpPublicChecker` | Öffentliche IP & Geolocation |
| `LocalHostsFileChecker` | `/etc/hosts` auf verdächtige Einträge |
| `BluetoothChecker` | Bluetooth-Status & Sichtbarkeit |
| `ExternalListenerChecker` | Extern erreichbare Dienste |
| `TorrentLeakChecker` | BitTorrent-Client IP-Leaks |
| `WifiSecurityChecker` | WLAN-Verschlüsselungsstandard |

</details>

<details>
<summary><b>🖥️ System-Kernel</b> — 7 Checker</summary>

| Checker | Was wird geprüft |
|:--|:--|
| `AslrChecker` | Address Space Layout Randomization |
| `CoreDumpChecker` | Core-Dump-Konfiguration |
| `KernelLockdownChecker` | Kernel Lockdown Mode |
| `KernelModuleChecker` | Geladene Kernel-Module |
| `SecureBootChecker` | Secure Boot Status |
| `DisplayServerChecker` | Display-Server Erkennung |
| `HostnameTimezoneChecker` | Hostname & Zeitzone |

</details>

<details>
<summary><b>🪟 Windows</b> — 10 Checker</summary>

| Checker | Was wird geprüft |
|:--|:--|
| `WindowsDefenderChecker` | Defender-Status & Echtzeit-Schutz |
| `WindowsFirewallChecker` | Windows Firewall Profile |
| `WindowsBitLockerChecker` | BitLocker-Verschlüsselung |
| `WindowsSecureBootChecker` | UEFI Secure Boot |
| `WindowsPrivacyChecker` | Telemetrie & Datenschutzeinstellungen |
| `WindowsAccountProtectionChecker` | Kontoschutz & PIN-Status |
| `WindowsCredentialProtectionChecker` | Credential Guard |
| `WindowsRemoteAccessChecker` | RDP & Remote-Zugriffe |
| `WindowsStartupPersistenceChecker` | Autostart-Einträge |
| `WindowsWirelessChecker` | WLAN-Sicherheit |

</details>

---

## Schnellstart

**Portabel (kein Install nötig)** — Lade das Paket vom [Release](https://github.com/SolidStateNetwork/OpSecAuditTool/releases/latest):

```bash
tar -xzf OpSecAuditTool-v1.1.0-linux-x64.tar.gz
./OpSecAuditTool
```

**Aus dem Source:**

```bash
git clone https://github.com/SolidStateNetwork/OpSecAuditTool.git
cd OpSecAuditTool
dotnet run           # .NET 10 SDK
```

Beide Pakete sind Self-Contained (inkl. .NET Runtime) — SHA-256 im Release.

---

## Architektur

```
┌─────────────────────────────────────────────────────────────┐
│  Avalonia UI  ·  Cyber-Terminal Theme  ·  Live-Konsole      │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  ConsoleLogPresenter (80ms Batch · Auto-Scroll)       │  │
│  └───────────────────────┬───────────────────────────────┘  │
│                          │                                  │
│  ┌───────────────────────▼───────────────────────────────┐  │
│  │  AuditRunnerViewModel (async · SemaphoreSlim)         │  │
│  └───────────────────────┬───────────────────────────────┘  │
└──────────────────────────┼──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│  OpSecCore · 67 Checker · Quick-Fix Engine · BackupService  │
│  Security │ Forensics │ Diagnostics │ Network │ System │ Win│
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│  OS / Kernel (User-Space only)                              │
│  /proc · ss · torrc · dbus · ~/.ssh · ~/.gnupg · ~Browser   │
└─────────────────────────────────────────────────────────────┘
```

---

## Lizenz

[MIT](LICENSE) · Contributions willkommen → [CONTRIBUTING.md](CONTRIBUTING.md) · Sicherheitslücken → [SECURITY.md](SECURITY.md)

<p align="center">
  <sub>Built with 🛡️ by <b>SolidStateNetwork</b> · No telemetry · No cloud · No sudo · Your data stays yours.</sub>
</p>
