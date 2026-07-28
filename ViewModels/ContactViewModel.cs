using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpSecAuditTool.Models;
using OpSecAuditTool.Services;

namespace OpSecAuditTool.ViewModels;

/// <summary>
/// Sub-ViewModel für Kontaktdaten und Zwischenablage-Aktionen.
/// </summary>
public sealed partial class ContactViewModel : ObservableObject
{
    [ObservableProperty] private string _xmppAddress;
    [ObservableProperty] private string _pgpKey;

    public ContactViewModel()
    {
        ContactInfo info = ContactService.GetContactInfo();
        _xmppAddress = info.XmppAddress;
        _pgpKey = info.PgpKey;
    }

    [RelayCommand]
    public async Task CopyXmpp() => await ClipboardService.CopyToClipboardAsync(XmppAddress, "XMPP-Kontaktdaten");

    [RelayCommand]
    public async Task CopyPgp() => await ClipboardService.CopyToClipboardAsync(PgpKey, "Public PGP-Key");
}
