using System;
using System.Text;
using Content.Server.Administration;
using Content.Server.AI.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.AI.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class FactionCommand : IConsoleCommand
    {
        public string Command => "factions";
        public string Description => Loc.GetString("faction-command-description");
        public string Help => Loc.GetString("faction-command-help-text");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
            {
                var result = new StringBuilder();
                foreach (Faction value in Enum.GetValues(typeof(Faction)))
                {
                    if (value == Faction.None)
                        continue;
                    result.Append(value + "\n");
                }

                shell.WriteLine(result.ToString());
                return;
            }

            if (args.Length < 2)
            {
                shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            if (!Enum.TryParse(args[0], true, out Faction faction))
            {
                shell.WriteLine(Loc.GetString("faction-command-invalid-faction-error"));
                return;
            }

            Faction targetFaction;

            switch (args[1])
            {
                case "friendly":
                    if (args.Length < 3)
                    {
                        shell.WriteLine(Loc.GetString("faction-command-no-target-faction-error"));
                        return;
                    }

                    if (!Enum.TryParse(args[2], true, out targetFaction))
                    {
                        shell.WriteLine(Loc.GetString("faction-command-invalid-target-faction-error"));
                        return;
                    }

                    EntitySystem.Get<AiFactionTagSystem>().MakeFriendly(faction, targetFaction);
                    shell.WriteLine(Loc.GetString("shell-command-success"));
                    break;
                case "hostile":
                    if (args.Length < 3)
                    {
                        shell.WriteLine(Loc.GetString("faction-command-no-target-faction-error"));
                        return;
                    }

                    if (!Enum.TryParse(args[2], true, out targetFaction))
                    {
                        shell.WriteLine(Loc.GetString("faction-command-invalid-target-faction-error"));
                        return;
                    }

                    EntitySystem.Get<AiFactionTagSystem>().MakeHostile(faction, targetFaction);
                    shell.WriteLine(Loc.GetString("shell-command-success"));
                    break;
                case "list":
                    shell.WriteLine(EntitySystem.Get<AiFactionTagSystem>().GetHostileFactions(faction).ToString());
                    break;
                default:
                    shell.WriteLine(Loc.GetString("faction-command-unknown-faction-argument-error"));
                    break;
            }
        }
    }
}
