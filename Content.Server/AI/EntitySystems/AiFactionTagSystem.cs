using System;
using System.Collections.Generic;
using Content.Server.AI.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server.AI.EntitySystems
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

        private readonly Dictionary<Faction, Faction> _hostileFactions = new();

        public override void Initialize()
        {
            base.Initialize();
            var protoManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var faction in protoManager.EnumeratePrototypes<AiFactionPrototype>())
            {
                if (Enum.TryParse(faction.ID,  out Faction @enum))
                {
                    var parsedFaction = Faction.None;

                    foreach (var hostile in faction.Hostile)
                    {
                        if (Enum.TryParse(hostile, out Faction parsedHostile))
                        {
                            parsedFaction |= parsedHostile;
                        }
                        else
                        {
                            Logger.Error($"Unable to parse hostile faction {hostile} for {faction.ID}");
                        }
                    }

                    _hostileFactions[@enum] = parsedFaction;
                }
                else
                {
                    Logger.Error($"Unable to parse AI faction {faction.ID}");
                }
            }
        }

        public Faction GetHostileFactions(Faction faction) => _hostileFactions.TryGetValue(faction, out var hostiles) ? hostiles : Faction.None;

        public Faction GetFactions(EntityUid entity) =>
            EntityManager.TryGetComponent(entity, out AiFactionTagComponent? factionTags)
            ? factionTags.Factions
            : Faction.None;

        public IEnumerable<EntityUid> GetNearbyHostiles(EntityUid entity, float range)
        {
            var ourFaction = GetFactions(entity);
            var hostile = GetHostileFactions(ourFaction);
            if (ourFaction == Faction.None || hostile == Faction.None)
            {
                yield break;
            }

            foreach (var component in EntityManager.EntityQuery<AiFactionTagComponent>(true))
            {
                if ((component.Factions & hostile) == 0)
                    continue;
                if (EntityManager.GetComponent<TransformComponent>(component.Owner).MapID != EntityManager.GetComponent<TransformComponent>(entity).MapID)
                    continue;
                if (!EntityManager.GetComponent<TransformComponent>(component.Owner).MapPosition.InRange(EntityManager.GetComponent<TransformComponent>(entity).MapPosition, range))
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

    [Flags]
    public enum Faction
    {
        None = 0,
        NanoTrasen = 1 << 0,
        SimpleHostile = 1 << 1,
        SimpleNeutral = 1 << 2,
        Syndicate = 1 << 3,
        Xeno = 1 << 4,
    }
}
