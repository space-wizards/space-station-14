// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Robust.Server.ServerStatus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared;
using Robust.Shared.Console;
using Robust.Server.Console;
using Robust.Shared.Utility;
using Content.Server.SS220.BackendApi;
using Robust.Server.Player;

namespace Content.Server.SS220.BackEndApi
{
    public sealed partial class ServerControlController
    {
        [Dependency] private readonly IStatusHost _statusHost = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IServerConsoleHost _serverConsoleHost = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private string? _watchdogToken;
        private string? _watchdogKey;

        private ISawmill _sawmill = default!;

        private static string ConsoleCommand => "/console-command";
        private static string PlayersCountCommand => "/players";

        private readonly HashSet<string> _serverCommands = new() { ConsoleCommand, PlayersCountCommand };

        public void Initialize()
        {
            _configurationManager.OnValueChanged(CVars.WatchdogToken, _ => UpdateToken());
            _configurationManager.OnValueChanged(CVars.WatchdogKey, _ => UpdateToken());

            UpdateToken();
        }

        public void PostInitialize()
        {
            _sawmill = Logger.GetSawmill("serverController");
            _statusHost.AddHandler(BackRequestHandler);
        }

        private async Task<bool> BackRequestHandler(IStatusHandlerContext context)
        {
            if (context.RequestMethod != HttpMethod.Post || !_serverCommands.Contains(context.Url!.AbsolutePath))
            {
                return false;
            }

            try
            {
                if (context.Url!.AbsolutePath == ConsoleCommand)
                {
                    await ConsoleCommandHandler(context);
                }
                else if (context.Url!.AbsolutePath == PlayersCountCommand)
                {
                    await PlayersCountHandler(context);
                }
            }
            catch (Exception exc)
            {
                _sawmill.Error(exc.Message);

                await context.RespondAsync(exc.Message, HttpStatusCode.InternalServerError);
            }

            return true;
        }

        private async Task PlayersCountHandler(IStatusHandlerContext context)
        {
            if (!await TryAuth(context))
            {
                return;
            }

            await context.RespondAsync(_playerManager.PlayerCount.ToString(), HttpStatusCode.OK);
        }

        private async Task<bool> TryAuth(IStatusHandlerContext context)
        {
            var auth = context.RequestHeaders["WatchdogToken"];

            if (auth != _watchdogToken)
            {
                _sawmill.Info(@"Failed auth: ""{0}"" vs ""{1}""", auth, _watchdogToken);
                await context.RespondErrorAsync(HttpStatusCode.Unauthorized);
                return false;
            }

            return true;
        }

        private async Task ConsoleCommandHandler(IStatusHandlerContext context)
        {
            if (!await TryAuth(context))
            {
                return;
            }

            var command = string.Empty;

            try
            {
                command = await context.RequestBodyJsonAsync<string>();
            }
            catch (Exception exc)
            {
                await context.RespondAsync($"Error on comand parse. {Environment.NewLine}{exc.Message}", HttpStatusCode.BadRequest);

                return;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                await context.RespondAsync("Command can't be empty", HttpStatusCode.BadRequest);

                return;
            }

            var args = new List<string>();

            CommandParsing.ParseArguments(command, args);

            var commandName = args[0];

            if (!_serverConsoleHost.AvailableCommands.TryGetValue(commandName, out var conCmd))
            {
                await context.RespondAsync($"Unknown command '{commandName}'", HttpStatusCode.BadRequest);

                return;
            }

            args.RemoveAt(0);
            var cmdArgs = args.ToArray();

            _taskManager.RunOnMainThread(async () => await RunConsoleCommand(conCmd, command, cmdArgs, context));
        }

        private async Task RunConsoleCommand(IConsoleCommand conCmd, string command, string[] cmdArgs, IStatusHandlerContext context)
        {
            try
            {
                var shell = new ConsoleShell(_serverConsoleHost, session: null, isLocal: true);

                var controllerConsole = new ControllerConsole(shell);

                conCmd.Execute(controllerConsole, command, cmdArgs);

                if (!string.IsNullOrWhiteSpace(controllerConsole.ErrorMsg))
                {
                    await context.RespondAsync(controllerConsole.ErrorMsg, HttpStatusCode.InternalServerError);
                }
                else
                {
                    await context.RespondAsync(controllerConsole.ResultMsg, HttpStatusCode.OK);
                }
            }
            catch (Exception exc)
            {
                _sawmill.Error(exc.Message);
            }
        }

        private void UpdateToken()
        {
            var tok = _configurationManager.GetCVar(CVars.WatchdogToken);
            var key = _configurationManager.GetCVar(CVars.WatchdogKey);

            _watchdogToken = string.IsNullOrEmpty(tok) ? null : tok;
            _watchdogKey = string.IsNullOrEmpty(key) ? null : key;
        }
    }
}
