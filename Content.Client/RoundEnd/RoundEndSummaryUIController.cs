using Content.Client.GameTicking.Managers;
using Content.Shared.DeadSpace.Arena;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.RoundEnd;

[UsedImplicitly]
public sealed class RoundEndSummaryUIController : UIController,
    IOnSystemLoaded<ClientGameTicker>
{
    [Dependency] private readonly IInputManager _input = default!;

    private RoundEndSummaryWindow? _window;
    private ArenaManifestEvent? _arenaManifest; // DS14

    private void ToggleScoreboardWindow(ICommonSession? session = null)
    {
        if (_window == null)
            return;

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.OpenCenteredRight();
            _window.ResumeManifestDollSnapshots(); // DS14
            _window.MoveToFront();
        }
    }

    public void OpenRoundEndSummaryWindow(RoundEndMessageEvent message)
    {
        // Don't open duplicate windows (mainly for replays).
        if (_window?.RoundId == message.RoundId)
            return;

        _window?.Close(); // DS14

        _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, EntityManager);

        if (_arenaManifest != null) // DS14
            _window.SetArenaManifest(_arenaManifest); // DS14
    }

    public void OnSystemLoaded(ClientGameTicker system)
    {
        _input.SetInputCommand(ContentKeyFunctions.ToggleRoundEndSummaryWindow,
            InputCmdHandler.FromDelegate(ToggleScoreboardWindow));
    }

    // DS14-Start
    public override void Initialize()
    {
        SubscribeNetworkEvent<ArenaManifestEvent>(OnArenaManifest);
    }

    private void OnArenaManifest(ArenaManifestEvent ev, EntitySessionEventArgs args)
    {
        _arenaManifest = ev;
        _window?.SetArenaManifest(ev);
    }
    // DS14-End
}
