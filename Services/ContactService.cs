using OpSecAuditTool.Models;

namespace OpSecAuditTool.Services;

/// <summary>
/// Stellt Kontaktdaten und kryptographische Schlüssel zur Verfügung.
/// </summary>
public static class ContactService
{
    private const string DefaultXmpp = "SolidStateNetwork@xmpp.is";
    private const string DefaultPgpKey =
        """
        -----BEGIN PGP PUBLIC KEY BLOCK-----

        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
        wiEbLt2oQSrP
        =Gv/m
        -----END PGP PUBLIC KEY BLOCK-----
        """;

    public static ContactInfo GetContactInfo() => new(DefaultXmpp, DefaultPgpKey);
}
