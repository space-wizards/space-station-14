using Content.Client.Eui;
using Content.Shared.Revolutionary;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Revolutionary.UI;

[UsedImplicitly]
public sealed class BecomeRevEui : BaseEui
{
    private readonly BecomeRevWindow _window;

    public BecomeRevEui()
    {
        _window = new BecomeRevWindow();

        _window.DenyButton.OnPressed += _ =>
        {
            SendMessage(new BecomeRevChoiceMessage(BecomeRevUiButton.Deny));
            _window.Close();
        };

        _window.OnClose += () => SendMessage(new BecomeRevChoiceMessage(BecomeRevUiButton.Deny));

        _window.AcceptButton.OnPressed += _ =>
        {
            SendMessage(new BecomeRevChoiceMessage(BecomeRevUiButton.Accept));
            _window.Close();
        };

        Timer.Spawn(10000, () => SendMessage(new BecomeRevChoiceMessage(BecomeRevUiButton.Deny)));
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

}
