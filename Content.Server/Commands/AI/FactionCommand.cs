using System;
using System.Text;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.AI;
using Content.Server.GameObjects.EntitySystems.AI;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;

namespace Content.Server.Commands.AI
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class FactionCommand : IClientCommand
    {
        public string Command => "factions";
        public string Description => "Update / list factional relationships for NPCs.";
        public string Help => "faction <source> <friendly/hostile> target\n" +
                              "faction <source> list: hostile factions";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
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

                shell.SendText(player, result.ToString());
                return;
            }

            if (args.Length < 2)
            {
                shell.SendText(player, Loc.GetString("Need more args"));
                return;
            }

            if (!Enum.TryParse(args[0], true, out Faction faction))
            {
                shell.SendText(player, Loc.GetString("Invalid faction"));
                return;
            }

            Faction targetFaction;

            switch (args[1])
            {
                case "friendly":
                    if (args.Length < 3)
                    {
                        shell.SendText(player, Loc.GetString("Need to supply a target faction"));
                        return;
                    }

                    if (!Enum.TryParse(args[2], true, out targetFaction))
                    {
                        shell.SendText(player, Loc.GetString("Invalid target faction"));
                        return;
                    }

                    EntitySystem.Get<AiFactionTagSystem>().MakeFriendly(faction, targetFaction);
                    shell.SendText(player, Loc.GetString("Command successful"));
                    break;
                case "hostile":
                    if (args.Length < 3)
                    {
                        shell.SendText(player, Loc.GetString("Need to supply a target faction"));
                        return;
                    }

                    if (!Enum.TryParse(args[2], true, out targetFaction))
                    {
                        shell.SendText(player, Loc.GetString("Invalid target faction"));
                        return;
                    }

                    EntitySystem.Get<AiFactionTagSystem>().MakeHostile(faction, targetFaction);
                    shell.SendText(player, Loc.GetString("Command successful"));
                    break;
                case "list":
                    shell.SendText(player, EntitySystem.Get<AiFactionTagSystem>().GetHostileFactions(faction).ToString());
                    break;
                default:
                    shell.SendText(player, Loc.GetString("Unknown faction arg"));
                    break;
            }

            return;
        }
    }
}