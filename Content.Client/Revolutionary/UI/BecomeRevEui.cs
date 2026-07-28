using System.Threading;
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
    // DS14-start
    private readonly CancellationTokenSource _timeoutCancellation = new();
    private bool _choiceSent;
    // DS14-end

    public BecomeRevEui()
    {
        _window = new BecomeRevWindow();

        // DS14-start
        _window.DenyButton.OnPressed += _ => Submit(BecomeRevUiButton.Deny);
        _window.AcceptButton.OnPressed += _ => Submit(BecomeRevUiButton.Accept);
        _window.OnClose += () =>
        {
            if (!_choiceSent)
                Submit(BecomeRevUiButton.Deny);
        };

        Timer.Spawn(10000, () => Submit(BecomeRevUiButton.Deny), _timeoutCancellation.Token);
        // DS14-end
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        // DS14-start
        _choiceSent = true;
        _timeoutCancellation.Cancel();
        // DS14-end
        _window.Close();
    }

    // DS14-start
    private void Submit(BecomeRevUiButton choice)
    {
        if (_choiceSent)
            return;

        _choiceSent = true;
        _timeoutCancellation.Cancel();
        SendMessage(new BecomeRevChoiceMessage(choice));
        _window.Close();
    }
    // DS14-end
}
