namespace OpSecAuditTool.Models;

/// <summary>
/// Domain-Modell für Kontakt- und Schlüsselinformationen.
/// </summary>
public sealed record ContactInfo(string XmppAddress, string PgpKey);
