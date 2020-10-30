#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.EntitySystems.StationEvents;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;

namespace Content.Server.StationEvents
{
    [AdminCommand(AdminFlags.Server)]
    public sealed class StationEventCommand : IClientCommand
    {
        public string Command => "events";
        public string Description => "Provides admin control to station events";
        public string Help => "events <list/pause/resume/stop/run <eventname/random>>\n" +
                              "list: return all event names that can be run\n " +
                              "pause: stop all random events from running\n" +
                              "resume: allow random events to run again\n" +
                              "run: start a particular event now; <eventname> is case-insensitive and not localized";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length == 0)
            {
                shell.SendText(player, "Need more args");
                return;
            }

            if (args[0] == "list")
            {
                var resultText = "Random\n" + EntitySystem.Get<StationEventSystem>().GetEventNames();
                shell.SendText(player, resultText);
                return;
            }

            // Didn't use a "toggle" so it's explicit
            if (args[0] == "pause")
            {
                var stationEventSystem = EntitySystem.Get<StationEventSystem>();

                if (!stationEventSystem.Enabled)
                {
                    shell.SendText(player, Loc.GetString("Station events are already paused"));
                    return;
                }
                else
                {
                    stationEventSystem.Enabled = false;
                    shell.SendText(player, Loc.GetString("Station events paused"));
                    return;
                }
            }

            if (args[0] == "resume")
            {
                var stationEventSystem = EntitySystem.Get<StationEventSystem>();

                if (stationEventSystem.Enabled)
                {
                    shell.SendText(player, Loc.GetString("Station events are already running"));
                    return;
                }
                else
                {
                    stationEventSystem.Enabled = true;
                    shell.SendText(player, Loc.GetString("Station events resumed"));
                    return;
                }
            }

            if (args[0] == "stop")
            {
                var resultText = EntitySystem.Get<StationEventSystem>().StopEvent();
                shell.SendText(player, resultText);
                return;
            }

            if (args[0] == "run" && args.Length == 2)
            {
                var eventName = args[1];
                string resultText;

                if (eventName == "random")
                {
                    resultText = EntitySystem.Get<StationEventSystem>().RunRandomEvent();
                }
                else
                {
                    resultText = EntitySystem.Get<StationEventSystem>().RunEvent(eventName);
                }

                shell.SendText(player, resultText);
                return;
            }

            shell.SendText(player, Loc.GetString("Invalid events command"));
            return;
        }
    }
}
