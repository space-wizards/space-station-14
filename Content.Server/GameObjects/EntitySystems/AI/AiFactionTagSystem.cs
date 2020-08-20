using System.Collections.Generic;
using Content.Server.GameObjects.Components.AI;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.AI
{
    /// <summary>
    ///     Outlines faction relationships with each other for AI.
    /// </summary>
    public sealed class AiFactionTagSystem : EntitySystem
    {
        
        // TODO: Settable via commands
        public Faction GetHostileFactions(Faction faction) => _hostileFactions.TryGetValue(faction, out var hostiles) ? hostiles : Faction.None;
        
        private Dictionary<Faction, Faction> _hostileFactions = new Dictionary<Faction, Faction>
        {
            {Faction.NanoTransen, 
                Faction.Syndicate | Faction.Xeno},
            {Faction.SimpleHostile,
                Faction.NanoTransen | Faction.Syndicate
            },
            // What makes a man turn neutral?
            {Faction.SimpleNeutral,
                Faction.None
            },
            {Faction.Syndicate,
                Faction.NanoTransen | Faction.Xeno},
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
            if (hostile == Faction.None)
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
    }
}