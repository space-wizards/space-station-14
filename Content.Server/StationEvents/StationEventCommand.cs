using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using System.Linq;
using System.Text;

namespace Content.Server.StationEvents
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class StationEventCommand : IConsoleCommand
    {
        public string Command => "events";
        public string Description => Loc.GetString("cmd-events-desc");
        public string Help => Loc.GetString("cmd-events-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (!args.Any())
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number") + $"\n{Help}");
                return;
            }

            switch (args.First())
            {
                case "list":
                    List(shell, player);
                    break;
                case "running":
                    Running(shell, player);
                    break;
                // Didn't use a "toggle" so it's explicit
                case "pause":
                    Pause(shell, player);
                    break;
                case "resume":
                    Resume(shell, player);
                    break;
                case "stop":
                    Stop(shell, player);
                    break;
                case "run":
                    if (args.Length != 2)
                    {
                        shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                                                      ("properAmount", 2),
                                                      ("currentAmount", args.Length))
                                        + $"\n{Help}");
                        break;
                    }

                    Run(shell, player, args[1]);
                    break;
                default:
                    shell.WriteLine(Loc.GetString($"shell-invalid-command-specific.", ("commandName", "events")) + $"\n{Help}");
                    break;
            }
        }

        private void Run(IConsoleShell shell, IPlayerSession? player, string eventName)
        {
            var stationSystem = EntitySystem.Get<StationEventSystem>();

            var resultText = eventName == "random"
                ? stationSystem.RunRandomEvent()
                : stationSystem.RunEvent(eventName);

            shell.WriteLine(resultText);
        }

        private void Running(IConsoleShell shell, IPlayerSession? player)
        {
            var eventName = EntitySystem.Get<StationEventSystem>().CurrentEvent?.Name;
            if (!string.IsNullOrEmpty(eventName))
            {
                shell.WriteLine(eventName);
            }
            else
            {
                shell.WriteLine(Loc.GetString("cmd-events-none-running"));
            }
        }

        private void List(IConsoleShell shell, IPlayerSession? player)
        {
            var events = EntitySystem.Get<StationEventSystem>();
            var sb = new StringBuilder();

            sb.AppendLine(Loc.GetString("cmd-events-list-random"));

            foreach (var stationEvents in events.StationEvents)
            {
                sb.AppendLine(stationEvents.Name);
            }

            shell.WriteLine(sb.ToString());
        }

        private void Pause(IConsoleShell shell, IPlayerSession? player)
        {
            var stationEventSystem = EntitySystem.Get<StationEventSystem>();

            if (!stationEventSystem.Enabled)
            {
                shell.WriteLine(Loc.GetString("cmd-events-already-paused"));
            }
            else
            {
                stationEventSystem.Enabled = false;
                shell.WriteLine(Loc.GetString("cmd-events-paused"));
            }
        }

        private void Resume(IConsoleShell shell, IPlayerSession? player)
        {
            var stationEventSystem = EntitySystem.Get<StationEventSystem>();

            if (stationEventSystem.Enabled)
            {
                shell.WriteLine(Loc.GetString("cmd-events-already-running"));
            }
            else
            {
                stationEventSystem.Enabled = true;
                shell.WriteLine(Loc.GetString("cmd-events-resumed"));
            }
        }

        private void Stop(IConsoleShell shell, IPlayerSession? player)
        {
            var resultText = EntitySystem.Get<StationEventSystem>().StopEvent();
            shell.WriteLine(resultText);
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = new[]
                {
                    "list",
                    "running",
                    "pause",
                    "resume",
                    "stop",
                    "run"
                };

                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-events-arg-subcommand"));
            }

            var command = args[0];

            if (args.Length != 2)
                return CompletionResult.Empty;

            if (command == "run")
            {
                var system = EntitySystem.Get<StationEventSystem>();
                var options = new[] { "random" }.Concat(
                    system.StationEvents.Select(e => e.Name).OrderBy(e => e));

                return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-events-arg-run-eventName"));
            }

            return CompletionResult.Empty;
        }
    }
}
