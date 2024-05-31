using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.NukeOps;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.NukeOps;

[UsedImplicitly]
public sealed class WarDeclaratorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;

    [ViewVariables]
    private WarDeclaratorWindow? _window;

    public WarDeclaratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        _window = new WarDeclaratorWindow(_gameTiming, _localizationManager);
        if (State != null)
            UpdateState(State);

        _window.OpenCentered();

        _window.OnClose += Close;
        _window.OnActivated += OnWarDeclaratorActivated;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not WarDeclaratorBoundUserInterfaceState cast)
            return;

        _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }

    private void OnWarDeclaratorActivated(string message)
    {
        var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
        var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
        SendMessage(new WarDeclaratorActivateMessage(msg));
    }
}
