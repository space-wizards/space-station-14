using Content.Client.Eui;
using Content.Shared._Offbrand.MMI;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._Offbrand.MMI;

[UsedImplicitly]
public sealed class MMIExtractorEui : BaseEui
{
    private readonly MMIExtractorMenu _menu;

    public MMIExtractorEui()
    {
        _menu = new MMIExtractorMenu();

        _menu.DenyButton.OnPressed += _ =>
        {
            SendMessage(new MMIExtractorMessage(false));
            _menu.Close();
        };

        _menu.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new MMIExtractorMessage(true));
            _menu.Close();
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        SendMessage(new MMIExtractorMessage(false));
        _menu.Close();
    }
}
