using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        /// <summary>
        ///     Used for thread safety, given <see cref="IStatusHost.OnStatusRequest"/> is called from another thread.
        /// </summary>
        private readonly object _statusShellLock = new();

        /// <summary>
        ///     Round start time in UTC, for status shell purposes.
        /// </summary>
        [ViewVariables]
        private DateTime _roundStartDateTime;

        /// <summary>
        ///     For access to CVars in status responses.
        /// </summary>
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        [Dependency] private readonly IStatusHost _status = default!;

        private bool _statusPlayerListEnabled;

        public event Action<JsonNode>? OnManifestRequest;

        private void InitializeStatusShell()
        {
            _status.OnStatusRequest += GetStatusResponse;
            _status.AddHandler(ManifestHandler);
            _configurationManager.OnValueChanged(CCVars.StatusPlayerListEnabled, b => _statusPlayerListEnabled = b, true);
        }

        private async Task<bool> ManifestHandler(IStatusHandlerContext context)
        {
            if (!context.IsGetLike || context.Url!.AbsolutePath != "/ss14/manifest")
            {
                return false;
            }

            var jObject = new JsonObject();

            if (_statusPlayerListEnabled)
            {
                var arr = new JsonArray();

                foreach (var session in _playerManager.ServerSessions.ToList())
                {
                    arr.Add(session.Name);
                }

                jObject["playerList"] = arr;
            }

            OnManifestRequest?.Invoke(jObject);

            await context.RespondJsonAsync(jObject);

            return true;
        }

        private void GetStatusResponse(JsonNode jObject)
        {
            // This method is raised from another thread, so this better be thread safe!
            lock (_statusShellLock)
            {
                jObject["name"] = _baseServer.ServerName;
                jObject["players"] = _playerManager.PlayerCount;
                jObject["soft_max_players"] = _cfg.GetCVar(CCVars.SoftMaxPlayers);
                jObject["run_level"] = (int) _runLevel;
                if (_runLevel >= GameRunLevel.InRound)
                {
                    jObject["round_start_time"] = _roundStartDateTime.ToString("o");
                }
            }
        }
    }
}
