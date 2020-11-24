using System;
using System.Collections.Generic;
using System.Text;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.AI;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.EntitySystems.AI
{
    /// <summary>
    ///     Outlines faction relationships with each other for AI.
    /// </summary>
    public sealed class AiFactionTagSystem : EntitySystem
    {
        /*
         *    Currently factions are implicitly friendly if they are not hostile.
         *    This may change where specified friendly factions are listed. (e.g. to get number of friendlies in area).
         */

        public Faction GetHostileFactions(Faction faction) => _hostileFactions.TryGetValue(faction, out var hostiles) ? hostiles : Faction.None;

        private readonly Dictionary<Faction, Faction> _hostileFactions = new Dictionary<Faction, Faction>
        {
            {Faction.NanoTransen,
                Faction.SimpleHostile | Faction.Syndicate | Faction.Xeno},
            {Faction.SimpleHostile,
                Faction.NanoTransen | Faction.Syndicate
            },
            // What makes a man turn neutral?
            {Faction.SimpleNeutral,
                Faction.None
            },
            {Faction.Syndicate,
                Faction.NanoTransen | Faction.SimpleHostile | Faction.Xeno},
            {Faction.Xeno,
                Faction.NanoTransen | Faction.Syndicate},
        };

        public Faction GetFactions(IEntity entity) =>
            entity.TryGetComponent(out AiFactionTagComponent factionTags)
            ? factionTags.Factions
            : Faction.None;

        public IEnumerable<IEntity> GetNearbyHostiles(IEntity entity, float range)
        {
            var ourFaction = GetFactions(entity);
            var hostile = GetHostileFactions(ourFaction);
            if (ourFaction == Faction.None || hostile == Faction.None)
            {
                yield break;
            }

            foreach (var component in ComponentManager.EntityQuery<AiFactionTagComponent>())
            {
                if ((component.Factions & hostile) == 0)
                    continue;
                if (component.Owner.Transform.MapID != entity.Transform.MapID)
                    continue;
                if (!component.Owner.Transform.MapPosition.InRange(entity.Transform.MapPosition, range))
                    continue;

                yield return component.Owner;
            }
        }

        public void MakeFriendly(Faction source, Faction target)
        {
            if (!_hostileFactions.TryGetValue(source, out var hostileFactions))
            {
                return;
            }

            hostileFactions &= ~target;
            _hostileFactions[source] = hostileFactions;
        }

        public void MakeHostile(Faction source, Faction target)
        {
            if (!_hostileFactions.TryGetValue(source, out var hostileFactions))
            {
                _hostileFactions[source] = target;
                return;
            }

            hostileFactions |= target;
            _hostileFactions[source] = hostileFactions;
        }
    }

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
