using Content.Client.RoundEnd;
using Robust.Client.Input;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Client.GameTicking.Managers;
using Robust.Shared.Log;

namespace Content.Client.UserInterface.Scoreboard
{
    public sealed class ScoreboardWindow
    {
        private readonly ClientGameTicker _gameTicker;
        [Dependency] private readonly IInputManager _input = default!;
        private ISawmill _logger = default!;

        public ScoreboardWindow(ClientGameTicker gameTicker)
        {
            _gameTicker = gameTicker;

            _input.SetInputCommand(ContentKeyFunctions.OpenScoreboardWindow,
                InputCmdHandler.FromDelegate(_ =>
                {
                    _logger.Debug("We're getting this far.");
                    if (_gameTicker._window != null)
                        OpenScoreboardWindow(_gameTicker._window);
                        _logger.Debug("We're tryin.");
                }));
        }

        private void OpenScoreboardWindow(RoundEndSummaryWindow window)
        {
            if (window == null)
                return;

            window.OpenCenteredRight();
            window.MoveToFront();
        }
    }
}
