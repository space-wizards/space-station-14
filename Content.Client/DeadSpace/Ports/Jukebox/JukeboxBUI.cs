using Content.Shared.DeadSpace.Ports.Jukebox;
using Content.Shared.Popups;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.DeadSpace.Ports.Jukebox;

public sealed class JukeboxBUI : BoundUserInterface
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    private readonly WhiteJukeboxMenu? _window;

    public JukeboxBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        var sharedPopupSystem = _entityManager.System<SharedPopupSystem>();

        if (!_entityManager.TryGetComponent(owner, out WhiteJukeboxComponent? jukeboxComponent))
        {
            sharedPopupSystem.PopupEntity("Тут нет WhiteJukeboxComponent, звоните кодерам", owner);
            return;
        }

        _window = new WhiteJukeboxMenu(owner, jukeboxComponent);
        _window.RepeatButton.OnToggled += OnRepeatButtonToggled;
        _window.StopButton.OnPressed += OnStopButtonPressed;
        _window.EjectButton.OnPressed += OnEjectButtonPressed;
    }

    private void OnEjectButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        SendMessage(new JukeboxEjectRequest());
    }

    private void OnStopButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        SendMessage(new JukeboxStopRequest());
    }

    private void OnRepeatButtonToggled(BaseButton.ButtonToggledEventArgs obj)
    {
        SendMessage(new JukeboxRepeatToggled(obj.Pressed));
    }

    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            Close();
            return;
        }

        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }
}
