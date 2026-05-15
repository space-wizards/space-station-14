using Content.Client.Eui;
using Content.Shared.Ghost;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Ghost.UI;

[UsedImplicitly]
public sealed class ReturnToBodyEui : BaseEui
{
    private readonly ReturnToBodyMenu _menu;

    public ReturnToBodyEui()
    {
        _menu = new ReturnToBodyMenu();

        _menu.DenyButton.OnPressed += _ =>
        {
            SendMessage(new ReturnToBodyMessage(false));
            _menu.Close();
        };

        _menu.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new ReturnToBodyMessage(true));
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

        SendMessage(new ReturnToBodyMessage(false));
        _menu.Close();
    }

}
