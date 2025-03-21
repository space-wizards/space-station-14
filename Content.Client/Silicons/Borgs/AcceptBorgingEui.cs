using Content.Client.Eui;
using Content.Client.UserInterface.Controls;
using Content.Shared.Silicons.Borgs;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Silicons.Borgs;

[UsedImplicitly]
public sealed class AcceptBorgingEui : BaseEui
{
    private readonly ConfirmationWindow _window;

    public AcceptBorgingEui()
    {
        _window = new ConfirmationWindow(
            Loc.GetString("borg-borging-window-title"),
            Loc.GetString("borg-borging-window-desc"),
            Loc.GetString("borg-borging-window-accept"),
            Loc.GetString("borg-borging-window-deny"));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new AcceptBorgingEuiMessage(true));
            _window.Close();
        };

        _window.CancelButton.OnPressed += _ =>
        {
            SendMessage(new AcceptBorgingEuiMessage(false));
            _window.Close();
        };
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        SendMessage(new AcceptBorgingEuiMessage(false));
        _window.Close();
    }
}
