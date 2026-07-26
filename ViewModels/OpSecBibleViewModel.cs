using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpSecAuditTool.ViewModels;

/// <summary>
/// Inhaltlicher Abschnitt eines Kapitels der integrierten OpSec-Bibel.
/// </summary>
public sealed class BibleSection
{
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
}

/// <summary>
/// Kapitel mit Inhaltsübersicht und den dazugehörigen Textabschnitten.
/// </summary>
public sealed class OpSecBibleChapter
{
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public List<string> SubTopics { get; init; } = [];
    public List<BibleSection> Sections { get; init; } = [];
}

/// <summary>
/// Stellt Kapitel und aktuelle Auswahl für das eigenständige Bibel-Fenster bereit.
/// </summary>
public sealed partial class OpSecBibleViewModel : ObservableObject
{
    public ObservableCollection<OpSecBibleChapter> Chapters { get; } = new();

    [ObservableProperty]
    private OpSecBibleChapter? _selectedChapter;

    public OpSecBibleViewModel()
    {
        LoadChapters();
        SelectedChapter = Chapters.FirstOrDefault();
    }

    private void LoadChapters()
    {
        Chapters.Add(new OpSecBibleChapter
        {
            Number = 1,
            Title = "1. Kernprinzipien der Sicherheit",
            SubTopics = new() { "Bedrohungsmodellierung" },
            Sections = new()
            {
                new BibleSection { Title = "EINFÜHRUNG", Body = "„Zu argumentieren, dass dir das Recht auf Privatsphäre egal ist, weil du nichts zu verbergen hast, unterscheidet sich nicht davon zu sagen, dass dir die freie Meinungsäußerung egal ist, weil du nichts zu sagen hast.“\n\nBEVOR DU IRGENDETWAS TUST, musst du wissen, wovon du dich eigentlich verteidigst. OPSEC ist keine magische Liste von Regeln, die man blind befolgt. Es geht darum, kalkulierte Schritte basierend auf deinen spezifischen Risiken zu machen." },
                new BibleSection { Title = "WAS SCHÜTZST DU?", Body = "Bist du dabei, sensible persönliche Daten, vertrauliche Geschäftsinformationen, illegale Aktivitäten oder einfach nur dein Recht auf Privatsphäre zu schützen? Vielleicht bist du Journalist, Aktivist, Hacker oder einfach jemand, der es vorzieht, seine Dinge unter Verschluss zu halten. Unterschiedliche Werte erfordern unterschiedliche Schutzniveaus." },
                new BibleSection { Title = "WER HAT ES AUF DICH ABGESEHEN?", Body = "Hier wird es ernst. Es gibt einen großen Weltenunterschied zwischen der Vermeidung irgendwelcher Skriptkiddies in einem Forum und dem Ausweichen vor einer ausgewachsenen staatlichen Überwachungsoperation. Hier ist das kurze Spektrum der Bedrohungen:\n• Niedrige Stufe (Low-tier): Neugierige Freunde, Ex-Partner, zufällige Trolls, unfähige Skriptkiddies.\n• Mittlere Stufe (Mid-tier): Konkurrenten, Cyberkriminelle, Doxxer, Stalker.\n• Hohe Stufe (High-tier): Strafverfolgungsbehörden, Geheimdienste, Wirtschaftsspionage, staatliche Akteure.\nJede dieser Gruppen agiert unterschiedlich. Einige verlassen sich auf Social Engineering, andere nutzen technische Exploits, und wieder andere erzwingen sich einfach mit legaler Macht den Zugang." },
                new BibleSection { Title = "WAS PASSIERT, WENN SIE GEWINNEN?", Body = "Das sind die Kosten des Scheiterns. Was ist das Worst-Case-Szenario, wenn deine Daten, deine Identität oder dein Standort in die falschen Hände geraten? Doxxing und Belästigung? Verlust des Zugriffs auf deine Konten und digitalen Assets? Rechtliche Konsequenzen, Verhaftung oder Schlimmeres?" },
                new BibleSection { Title = "DEINE VERTEIDIGUNG AUFBAUEN", Body = "Bedrohungsmodellierung ist keine bloße Übung – sie bestimmt jede Entscheidung in deiner OpSec. Wenn deine größte Bedrohung ein schnüffelnder Ex ist, brauchst du keine Sicherheit auf NSA-Niveau. Stehst du jedoch einer staatlichen Institution gegenüber, brauchst du strenge Regeln." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 2,
            Title = "2. Grundlegende digitale Hygiene",
            SubTopics = new() { "Passwortsicherheit", "Datenverschlüsselung", "Mobiltelefonsicherheit" },
            Sections = new()
            {
                new BibleSection { Title = "PASSWORTSICHERHEIT", Body = "Wenn du Passwörter auf mehreren Plattformen wiederverwendest, machst du es jemandem viel zu leicht, deine Identität plattformübergreifend zu verknüpfen. Deshalb sind eindeutige Passwörter für jedes Konto ein absolutes Muss. Passwortmanager wie KeePassXC machen es einfach, starke und eindeutige Passwörter sicher zu generieren und zu speichern. Für noch mehr Sicherheit nutze Whonix im Offline-Modus, um Leaks zu verhindern." },
                new BibleSection { Title = "DATENVERSCHLÜSSELUNG", Body = "Um sensible Daten zu schützen, ist Verschlüsselung unerlässlich. Die Festplatte speichert nicht nur abgespeicherte Dateien, sondern auch diverse vom System generierte Daten wie Logs und temporäre Dateien, die deine Aktivitäten verraten können. Nutze stets Open-Source-Verschlüsselungssoftware wie VeraCrypt oder dm-crypt/LUKS." },
                new BibleSection { Title = "MOBILTELEFONSICHERHEIT", Body = "Verwende kein iPhone. Auch wenn iPhones sicher erscheinen mögen, sind sie stark in Apples Ökosystem integriert, das eine erhebliche Menge an Nutzerdaten sammelt. Apples Kontrolle über Hardware und Software kompromissert die Privatsphäre und macht es ungeeignet für Anonymität (geschlossener Quellcode, kein Root-Zugriff).\n\nZudem schwächen ROMs wie LineageOS die Anonymität, da sich der Bootloader nach dem Entsperren oft nicht wieder verriegeln lässt, was das Gerät für forensische Tools angreifbar macht.\n\nFür ein Höchstmaß an Sicherheit ist GrapheneOS die erste Wahl (beschränkt auf Pixel-Geräte ab Generation 6)." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 3,
            Title = "3. Erweiterte Datenschutzmaßnahmen",
            SubTopics = new() { "Sichere Kommunikation", "Schutz in sozialen Netzwerken", "Anonymes Surfen" },
            Sections = new()
            {
                new BibleSection { Title = "SICHERE KOMMUNIKATION", Body = "Nutze Anwendungen wie Signal oder – noch besser – Molly (molly.im), Session oder XMPP mit Onion-Domains und OTR-/OMEMO-Verschlüsselung. Diese bieten einen weitaus besseren Schutz als gängige Messenger. Wer noch Gmail, Outlook oder ähnliche Plattformen nutzt, riskiert eine Datenschutzkatastrophe." },
                new BibleSection { Title = "SCHUTZ IN SOZIALEN NETZWERKEN", Body = "Es ist nahezu unmöglich, soziale Medien komplett zu meiden, aber du kannst den Fußabdruck minimieren:\n• Lösche alte Accounts und erstelle neue über Tor.\n• Deaktiviere alle datenschutzrelevanten Telemetrie-Einstellungen.\n• Stelle alle Profile auf privat.\n• Nutze niemals deinen echten Namen, reale Fotos oder identifizierbare Details.\n• Verwende separate Geräte oder virtuelle Maschinen für jede Persona.\n• Nutze keine persönlichen Fotos oder Fensteraufnahmen als Profilbild.\n• Halte Bio-Felder leer.\n• Tagge keine Standorte.\n• Aktiviere 2FA für jeden Account.\n• Verwende überall unterschiedliche Benutzernamen." },
                new BibleSection { Title = "ANONYMES SURFEN", Body = "Mainstream-Browser wie Chrome oder Opera sammeln massenhaft Daten. Wechsle zu datenschutzfokussierten Browsern wie Brave oder LibreWolf. Für mehrere Accounts eignen sich Anti-Detection-Browser wie Dolphin{anty}, Incogniton oder Ghost Browser.\n\n• Suchmaschinen: Meide Google und nutze stattdessen DuckDuckGo oder SearXNG.\n• Empfohlene Erweiterungen: uBlock Origin, Privacy Badger, Cookie AutoDelete, NoScript, ClearURLs." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 4,
            Title = "4. Erweiterte Anonymisierungswerkzeuge",
            SubTopics = new() { "Tor-/I2P-Nutzung", "Anonyme Zahlungen", "Identitätsmanagement" },
            Sections = new()
            {
                new BibleSection { Title = "TOR- / I2P-NUTZUNG", Body = "Wenn du es mit der Anonymität ernst meinst, sind Tor oder I2P unverzichtbar. Timing-Angriffe sind an der Tagesordnung: Wenn ein Gegner genügend Relays kontrolliert, kann er Aktivitäten nachverfolgen. Achte auf bösartige Relays und deaktiviere JavaScript im Browser." },
                new BibleSection { Title = "ANONYME ZAHLUNGEN", Body = "Monero (XMR) ist aufgrund seiner Privatsphäre-Features eine der sichersten Optionen:\n• Option 1: P2P-Börsen wie BitValve, HodlHodl oder LocalCoinSwap (ohne KYC) nutzen und via Trocador in Monero tauschen.\n• Option 2: Dezentrale Börsen (DEXs) wie Haveno oder Bisq direkt nutzen.\nSende die Mittel stets an eine eigene, selbstgehostete Monero-Wallet auf einem sicheren, isolierten Gerät." },
                new BibleSection { Title = "IDENTITÄTSMANAGEMENT", Body = "Eine gefälschte Persona zu erstellen bedeutet, eine glaubwürdige Identität aufzubauen (Name, Alter, Standort). Nutze Tools wie den Fake Person Generator und KI-Bildgeneratoren wie Stable Diffusion oder Midjourney, statt Stock-Fotos zu verwenden." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 5,
            Title = "5. Physische Sicherheit",
            SubTopics = new() { "Sicherer Arbeitsplatz", "Vermeidung von Überwachung", "Notfallprotokolle" },
            Sections = new()
            {
                new BibleSection { Title = "SICHERER ARBEITSPLATZ ('HACKER-HÖHLE')", Body = "• Stationäre Konfiguration: Nutze einen Computer ausschließlich für sensible Operationen, idealerweise ohne Akku.\n• Not-Aus (Emergency Stop): Installiere einen Notausschalter, um sofort Strom zu kappen und Daten zu verschlüsseln.\n• Signalisolierung: Nutze Faradaysche Käfige oder Signalblockierungstaschen.\n• Kontrollierter Zugang: Halte den Raum verschlossen." },
                new BibleSection { Title = "GERÄTE VOLLSTÄNDIG ABSCHALTEN", Body = "Verschlüsselung nützt nichts im Standby-Modus – schalte Geräte nach der Nutzung immer komplett ab." },
                new BibleSection { Title = "UNSICHTBAR BLEIBEN & MOBILTELEFONE", Body = "Vermeide auffällige Kleidung und halte ein unauffälliges Profil. Schalte GPS niemals grundlos ein und meide unbekannte WLAN-Netzwerke. Nutze Faradaysche Taschen oder lass das Smartphone bei sensiblen Operationen komplett daheim." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 6,
            Title = "6. Verhaltensbiometrische Bedrohungen",
            SubTopics = new() { "Stilometrie", "Verhaltensbiometrisches Profiling", "Abwehrstrategien" },
            Sections = new()
            {
                new BibleSection { Title = "STILOMETRIE", Body = "Stilometrie analysiert Schreibmuster, um den Autor selbst unter Pseudonymen zu enttarnen. Verändere deine natürlichen Gewohnheiten bewusst und schreibe längere Texte vorab offline im Editor (Notepad), um Keylogging-Analysen zu verhindern." },
                new BibleSection { Title = "VERHALTENSBIOMETRISCHES PROFILING", Body = "Die zeitlichen Abstände zwischen Tastaturanschlägen (Keystroke-Timing) sind so einzigartig wie ein Fingerabdruck. Algorithmen benötigen nur wenige Minuten, um eine Person zu identifizieren." },
                new BibleSection { Title = "ABWEHRSTRATEGIEN", Body = "Nutze Text-Editor-Zwischenspeicherungen, Browser-Erweiterungen oder deaktiviere JavaScript, um Profiling zu erschweren." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 7,
            Title = "7. Finanzielle Anonymität",
            SubTopics = new() { "Anonyme Zahlungen", "Sichere Verwaltung von Einnahmen" },
            Sections = new()
            {
                new BibleSection { Title = "ANONYME ZAHLUNGEN", Body = "Nutze Monero (XMR) über KYC-freie P2P-Börsen und dezentrale Marktplätze, um jegliche Rückverfolgbarkeit über traditionelle Banken zu kappen." },
                new BibleSection { Title = "SICHERE VERWALTUNG VON EINNAHMEN", Body = "Vermeide auffälligen Wohlstand (keine Luxusautos oder Immobilienkäufe mit diesen Mitteln). Halte Einnahmen aus sensiblen Bereichen komplett aus dem traditionellen Bankensystem heraus und nutze Bargeld." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 8,
            Title = "8. Datenschutz & Sicheres Löschen",
            SubTopics = new() { "Sichere Speicherung", "Verschlüsselte Laufwerke" },
            Sections = new()
            {
                new BibleSection { Title = "SICHERE SPEICHERUNG", Body = "Das einfache Löschen einer Datei entfernt sie nicht – das Betriebssystem gibt lediglich den Speicherplatz frei. Nutze zertifizierte Löschmethoden wie NIST 800-88." },
                new BibleSection { Title = "VERSCHLÜSSELTE LAUFWERKE", Body = "Setze von Anfang an auf verschlüsselte Dateisysteme (LUKS/dm-crypt unter Linux, VeraCrypt unter Windows)." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 9,
            Title = "9. Mobil- & Gerätesicherheit",
            SubTopics = new() { "Sichere Betriebssysteme", "Vermeidung von Tracking" },
            Sections = new()
            {
                new BibleSection { Title = "SICHERE BETRIEBSSYSTEME", Body = "Nutze GrapheneOS auf Google-Pixel-Hardware, da diese extrem resistent gegen forensische Extraktionstools (z. B. Cellebrite) ist. Meide iPhones und unsichere Custom-ROMs." },
                new BibleSection { Title = "VERMEIDUNG VON TRACKING", Body = "Sei dir der Risiken von Mobilfunk-Triangulation bewusst. Schütze dich durch Faraday-Taschen oder indem du das Gerät bei sensiblen Aktionen abschaltest." }
            }
        });

        Chapters.Add(new OpSecBibleChapter
        {
            Number = 10,
            Title = "10. VPNs & Netzwerksicherheit",
            SubTopics = new() { "Auswahl eines VPNs", "Netzwerksicherheit", "Vermeidung von Tracking-Techniken" },
            Sections = new()
            {
                new BibleSection { Title = "AUSWAHL EINES VPNS", Body = "Achte auf eine strikte No-Logs-Richtlinie, Monero-Zahlungsoptionen und meide kostenlose Anbieter. Betrachte VPNs als Schutz für den Alltag, aber verlasse dich bei echter Anonymität nicht darauf." },
                new BibleSection { Title = "NETZWERKSICHERHEIT", Body = "• Wirf vom ISP bereitgestellte Standard-Router weg.\n• Nutze GL.INet, pfSense oder OpenWRT/OPNsense.\n• Ändere Standard-Zugangsdaten sofort.\n• Richte ein separates VLAN für IoT-Geräte ein.\n• Harte Firewall-Regeln: Ausgehend standardmäßig blockieren.\n• DNS-Sicherheit: Nutze Quad9 oder Mullvad-DNS.\n• WLAN: Nutze primär Ethernet oder mindestens WPA3." }
            }
        });

        Chapters.Add(CreateOpSecBibleChapter(11, "11. Metadaten in Bildern", new() { "EXIF-Datenrisiken", "Rauschen & Bild-Fingerprinting", "Quantisierungstabellen & Tracking" }));
        Chapters.Add(CreateOpSecBibleChapter(12, "12. Physische OPSEC", new() { "Sicherer Transport", "Datenschutz in der Öffentlichkeit", "Vermeidung von Gesichtserkennung", "Krisenmanagement", "Psychologische Verteidigung" }));
        Chapters.Add(CreateOpSecBibleChapter(13, "13. Notfall- & Krisenplanung", new() { "Notfallprotokolle", "Ausstiegsstrategien", "Langfristige Anpassung" }));
        Chapters.Add(CreateOpSecBibleChapter(14, "14. Strategisches Denken", new() { "Zukunftsplanung" }));
    }

    private static OpSecBibleChapter CreateOpSecBibleChapter(int number, string title, List<string> subTopics)
    {
        List<BibleSection> sections = number switch
        {
            11 => new()
            {
                new BibleSection { Title = "EXIF-DATENRISIKEN", Body = "EXIF-Metadaten (Kameramodell, Blende, Belichtungszeit, ISO, teils GPS-Koordinaten) sind in den meisten Bilddateien eingebettet. Vor der Veröffentlichung online müssen diese Daten zwingend entfernt werden." },
                new BibleSection { Title = "RAUSCHEN & BILD-FINGERPRINTING", Body = "Digitales Rauschen bildet ein einzigartiges Sensormuster, das Forensiker nutzen, um den Ursprung auf ein spezifisches Gerät zurückzuführen." },
                new BibleSection { Title = "QUANTISIERUNGSTABELLEN", Body = "JPEG-Kompressionstabellen dienen als digitale Signatur der verwendeten Software oder des Geräts." }
            },
            12 => new()
            {
                new BibleSection { Title = "SICHERER TRANSPORT", Body = "Nutze portable Datenträger mit Lanyards und leichten Abreißkabeln." },
                new BibleSection { Title = "DATENSCHUTZ IN DER ÖFFENTLICHKEIT", Body = "Halte ein unauffälliges Profil und vermeide risikoreiches Verhalten." },
                new BibleSection { Title = "GESICHTSERKENNUNG AUSTRICKSEN", Body = "Setze auf dezente Hilfsmittel (reflektierende Brillen, Masken)." },
                new BibleSection { Title = "KRISENMANAGEMENT", Body = "Sei auf das Unerwartete vorbereitet und halte Exit-Routen bereit." },
                new BibleSection { Title = "PSYCHOLOGISCHE VERTEIDIGUNG", Body = "Schütze dich vor Social Engineering, Infiltrationen, Honigtöpfen und Deepfakes unter Anwendung von Zero Trust." }
            },
            13 => new()
            {
                new BibleSection { Title = "NOTFALLPROTOKOLLE", Body = "• Kill-Switches: Sofortiges Kappen aller aktiven Sitzungen.\n• Evakuierungspläne: Vordefinierte Fluchtwege." },
                new BibleSection { Title = "AUSSTIEGSSTRATEGIEN", Body = "Bei totaler Kompromittierung: Alle Geräte zurücklassen und untertauchen." },
                new BibleSection { Title = "LANGFRISTIGE ANPASSUNG", Body = "Optimiere Tools und Taktiken kontinuierlich, um dem Gegner einen Schritt voraus zu sein." }
            },
            14 => new()
            {
                new BibleSection { Title = "STRATEGISCHES DENKEN & RISIKOMANAGEMENT", Body = "Gutes OPSEC erfordert proaktive Weitsicht statt reaktiver Panik:\n• Frühzeitiges Erkennen von Risiken.\n• Keine überstürzten Handlungen.\n• Gesamtschau bewahren.\n• Flexibilität und Anpassung." }
            },
            _ => new() { new BibleSection { Title = title, Body = "Inhalt folgt in Kürze..." } }
        };

        return new OpSecBibleChapter
        {
            Number = number,
            Title = title,
            SubTopics = subTopics,
            Sections = sections
        };
    }
}
