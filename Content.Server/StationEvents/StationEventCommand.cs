using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using System.Linq;

namespace Content.Server.StationEvents
{
    [AdminCommand(AdminFlags.Round)]
    public sealed class StationEventCommand : IConsoleCommand
    {
        public string Command => "events";
        public string Description => Loc.GetString("station-event-command-description");
        public string Help => Loc.GetString("station-event-command-help-text",
                                            ("runningHelp", Loc.GetString("station-event-command-running-help-text")),
                                            ("listHelp", Loc.GetString("station-event-command-list-help-text")),
                                            ("pauseHelp", Loc.GetString("station-event-command-pause-help-text")),
                                            ("resumeHelp", Loc.GetString("station-event-command-resume-help-text")),
                                            ("runHelp", Loc.GetString("station-event-command-run-help-text")));

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
                                        + $"\n{Loc.GetString("station-event-command-run-help-text")}");
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
                shell.WriteLine(Loc.GetString("No station event running"));
            }
        }

        private void List(IConsoleShell shell, IPlayerSession? player)
        {
            var resultText = Loc.GetString("station-event-command-event-list",
                                           ("otherEvents", EntitySystem.Get<StationEventSystem>().GetEventNames()));
            shell.WriteLine(resultText);
        }

        private void Pause(IConsoleShell shell, IPlayerSession? player)
        {
            var stationEventSystem = EntitySystem.Get<StationEventSystem>();

            if (!stationEventSystem.Enabled)
            {
                shell.WriteLine(Loc.GetString("station-event-command-events-already-paused-message"));
            }
            else
            {
                stationEventSystem.Enabled = false;
                shell.WriteLine(Loc.GetString("station-event-command-events-paused-message"));
            }
        }

        private void Resume(IConsoleShell shell, IPlayerSession? player)
        {
            var stationEventSystem = EntitySystem.Get<StationEventSystem>();

            if (stationEventSystem.Enabled)
            {
                shell.WriteLine(Loc.GetString("station-event-command-events-already-running-message"));
            }
            else
            {
                stationEventSystem.Enabled = true;
                shell.WriteLine(Loc.GetString("station-event-command-events-resumed-message"));
            }
        }

        private void Stop(IConsoleShell shell, IPlayerSession? player)
        {
            var resultText = EntitySystem.Get<StationEventSystem>().StopEvent();
            shell.WriteLine(resultText);
        }
    }
}
