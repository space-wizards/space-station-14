using System;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Newtonsoft.Json.Linq;
using Robust.Server;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.IoC;

namespace Content.Server
{
    /// <summary>
    ///     Tiny helper class to handle status messages. Nothing too complicated.
    /// </summary>
    public class StatusShell
    {
        private readonly IPlayerManager _playerManager;
        private readonly string _name;
        private GameRunLevel _runLevel;
        private DateTime _roundStartTime;

        public StatusShell()
        {
            _playerManager = IoCManager.Resolve<IPlayerManager>();
            var baseServer = IoCManager.Resolve<IBaseServer>();
            var gameTicker = IoCManager.Resolve<IGameTicker>();

            gameTicker.OnRunLevelChanged += _runLevelChanged;

            _name = baseServer.ServerName;
            IoCManager.Resolve<IStatusHost>().OnStatusRequest += _getResponse;
        }

        private void _getResponse(JObject jObject)
        {
            lock (this)
            {
                jObject["name"] = _name;
                jObject["players"] = _playerManager.PlayerCount;
                jObject["run_level"] = (int) _runLevel;
                if (_runLevel >= GameRunLevel.InRound)
                {
                    jObject["round_start_time"] = _roundStartTime.ToString("o");
                }
            }
        }

        private void _runLevelChanged(GameRunLevelChangedEventArgs eventArgs)
        {
            lock (this)
            {
                _runLevel = eventArgs.NewRunLevel;
                if (eventArgs.NewRunLevel == GameRunLevel.InRound)
                {
                    _roundStartTime = DateTime.UtcNow;
                }
            }
        }
    }
}
