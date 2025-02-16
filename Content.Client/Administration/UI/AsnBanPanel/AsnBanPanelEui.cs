using JetBrains.Annotations;
using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;

namespace Content.Client.Administration.UI.AsnBanPanel;

[UsedImplicitly]
public sealed class AsnBanPanelEui : BaseEui
{
    private AsnBanPanel AsnBanPanel { get; }

    public AsnBanPanelEui()
    {
        AsnBanPanel = new AsnBanPanel();
        AsnBanPanel.OnClose += () => SendMessage(new CloseEuiMessage());
        AsnBanPanel.BanSubmitted += (asn, minutes, reason, severity)
            => SendMessage(new AsnBanPanelEuiStateMsg.CreateAsnBanRequest(asn, minutes, reason, severity));
        AsnBanPanel.AsnChanged += asn => SendMessage(new AsnBanPanelEuiStateMsg.GetAsnInfoRequest(asn));
    }

    public override void Opened()
    {
        AsnBanPanel.OpenCentered();
    }

    public override void Closed()
    {
        AsnBanPanel.Close();
        AsnBanPanel.Dispose();
    }
}
