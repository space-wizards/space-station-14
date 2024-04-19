using Content.Shared.GameTicking;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.RoundEnd;

public sealed class RoundEndSummarySystem : EntitySystem
{
    private RoundEndSummaryWindow? _window;

    public override void Initialize()
    {
        CommandBinds.Builder.Bind(ContentKeyFunctions.ToggleRoundEndSummaryWindow, InputCmdHandler.FromDelegate(ToggleScoreboardWindow))
            .Register<RoundEndSummarySystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<RoundEndSummarySystem>();
    }

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
            _window.MoveToFront();
        }
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
