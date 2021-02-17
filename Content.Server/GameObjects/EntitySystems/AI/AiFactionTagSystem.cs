using System.Collections.Generic;
using Content.Server.GameObjects.Components.AI;
using Robust.Shared.GameObjects;

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

        private readonly Dictionary<Faction, Faction> _hostileFactions = new()
        {
            {Faction.NanoTrasen,
                Faction.SimpleHostile | Faction.Syndicate | Faction.Xeno},
            {Faction.SimpleHostile,
                Faction.NanoTrasen | Faction.Syndicate
            },
            // What makes a man turn neutral?
            {Faction.SimpleNeutral,
                Faction.None
            },
            {Faction.Syndicate,
                Faction.NanoTrasen | Faction.SimpleHostile | Faction.Xeno},
            {Faction.Xeno,
                Faction.NanoTrasen | Faction.Syndicate},
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

            foreach (var component in ComponentManager.EntityQuery<AiFactionTagComponent>(true))
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
}
