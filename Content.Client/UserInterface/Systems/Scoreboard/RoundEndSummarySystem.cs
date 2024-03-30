using Content.Client.RoundEnd;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems.Scoreboard;

public sealed class RoundEndSummarySystem : EntitySystem
{
    private RoundEndSummaryWindow? _window;

    public override void Initialize()
    {
        CommandBinds.Builder.Bind(ContentKeyFunctions.OpenScoreboardWindow, InputCmdHandler.FromDelegate(OpenScoreboardWindow))
            .Register<RoundEndSummarySystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RoundEndSummarySystem>();
    }

    private void OpenScoreboardWindow(ICommonSession? session = null)
    {
        if (_window == null)
            return;

        _window.OpenCenteredRight();
        _window.MoveToFront();
    }

    public void OpenRoundEndSummaryWindow(RoundEndMessageEvent message)
    {
        // Don't open duplicate windows (mainly for replays).
        if (_window?.RoundId == message.RoundId)
            return;

        _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, EntityManager);
    }
}
