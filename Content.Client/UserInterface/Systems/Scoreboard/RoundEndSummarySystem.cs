using Content.Client.RoundEnd;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Systems.Scoreboard;

public sealed class RoundEndSummarySystem : EntitySystem
{
    private RoundEndSummaryWindow? _window;

    public override void Initialize()
    {
        CommandBinds.Builder.Bind(ContentKeyFunctions.OpenScoreboardWindow,
            InputCmdHandler.FromDelegate(_ =>
            {
                if (_window != null)
                    OpenScoreboardWindow(_window);
            }))
            .Register<RoundEndSummarySystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RoundEndSummarySystem>();
    }

    private void OpenScoreboardWindow(RoundEndSummaryWindow window)
    {
        window.OpenCenteredRight();
        window.MoveToFront();
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
