using Content.Client.GameTicking.Managers;
using Content.Client.UserInterface.Controls;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.RoundEnd;

[UsedImplicitly]
public sealed class RoundEndSummaryUIController : UIController
{
    [Dependency] private readonly IInputManager _input = default!;

    public RoundEndSummaryWindow? Window;

    public void ToggleScoreboardWindow(ICommonSession? session = null)
    {
        if (Window == null)
            return;

        if (Window.IsOpen)
        {
            Window.Close();
        }
        else
        {
            Window.OpenCenteredRight();
            Window.MoveToFront();
        }
    }

    public void OpenRoundEndSummaryWindow(RoundEndMessageEvent message)
    {
        // Don't open duplicate windows (mainly for replays).
        if (Window?.RoundId == message.RoundId)
            return;

        Window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, EntityManager);
    }

    public void OnSystemLoaded(ClientGameTicker system)
    {
        _input.SetInputCommand(ContentKeyFunctions.ToggleRoundEndSummaryWindow,
            InputCmdHandler.FromDelegate(ToggleScoreboardWindow));
    }
}
