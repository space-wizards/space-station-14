using Newtonsoft.Json.Linq;
using Robust.Server.ServerStatus;
using Robust.Shared.IoC;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        private readonly object _statusShellLock = new();

        private void InitializeStatusShell()
        {
            IoCManager.Resolve<IStatusHost>().OnStatusRequest += GetStatusResponse;
        }

        private void GetStatusResponse(JObject jObject)
        {
            lock (_statusShellLock)
            {
                jObject["name"] = _baseServer.ServerName;
                jObject["players"] = _playerManager.PlayerCount;
                jObject["run_level"] = (int) _runLevel;
                if (_runLevel >= GameRunLevel.InRound)
                {
                    jObject["round_start_time"] = _roundStartTime.ToString("o");
                }
            }
        }
    }
}
