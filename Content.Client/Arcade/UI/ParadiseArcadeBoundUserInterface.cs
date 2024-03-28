using System.Threading.Tasks;
using Content.Shared.Arcade;
using JetBrains.Annotations;
using Robust.Client;
using Robust.Shared.Timing;
using Serilog;

namespace Content.Client.Arcade.UI;

[UsedImplicitly]
public sealed class ParadiseArcadeBoundUserInterface : BoundUserInterface
{
    [Dependency]
    private IGameController _gameController = default!;

    private ParadiseArcadeWindow? _window;

    public ParadiseArcadeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new();

        _window.OpenCentered();
        _window.OnClose += Close;
        _window.OnConnectButtonPressed = () =>
        {
            SendMessage(new ParadiesMessages.ParadiseArcadeConnectButtonPressedEvent());
        };
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is not ParadiesMessages.ParadiseArcadeConnectEvent connectEvent)
            return;

        _gameController.Redial(connectEvent.Destination, "Paradise arcade used to connect to another server.");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}
