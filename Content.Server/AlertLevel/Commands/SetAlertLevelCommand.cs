using System.Linq;
using Content.Server.Administration;
using Content.Server.AlertLevel;
using Content.Server.Players;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.AlertLevel.Commands
{
    [UsedImplicitly]
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SetAlertLevelCommand : IConsoleCommand
    {
        public string Command => "setalertlevel";
        public string Description => Loc.GetString("cmd-setalertlevel-desc");
        public string Help => Loc.GetString("cmd-setalertlevel-help");

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            var levelPrototype = IoCManager.Resolve<IPrototypeManager>().Index<AlertLevelPrototype>(AlertLevelSystem.DefaultAlertLevelSet);
            var levelNames = levelPrototype.Levels.Keys.ToArray();

            return args.Length switch
            {
                1 => CompletionResult.FromHintOptions(levelNames,
                    Loc.GetString("cmd-setalertlevel-hint-1")),
                2 => CompletionResult.FromHintOptions(CompletionHelper.Booleans,
                    Loc.GetString("cmd-setalertlevel-hint-2")),
                _ => CompletionResult.Empty,
            };
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var locked = false;
            if (args.Length > 1 && !bool.TryParse(args[1], out locked))
            {
                shell.WriteLine(Loc.GetString("shell-argument-must-be-boolean"));
                return;
            }

            var player = shell.Player as IPlayerSession;
            if (player?.AttachedEntity == null)
            {
                shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            var stationUid = EntitySystem.Get<StationSystem>().GetOwningStation(player.AttachedEntity.Value);
            if (stationUid == null)
            {
                shell.WriteLine(Loc.GetString("cmd-setalertlevel-invalid-grid"));
                return;
            }

            var level = args[0];
            EntitySystem.Get<AlertLevelSystem>().SetLevel(stationUid.Value, level, true, true, true, locked);
        }
    }
}
