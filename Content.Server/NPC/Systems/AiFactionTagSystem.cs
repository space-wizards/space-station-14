using Content.Server.NPC.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Systems
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

            // TODO: Yes I know this system is shithouse
            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(entity);

            foreach (var component in EntityManager.EntityQuery<AiFactionTagComponent>(true))
            {
                if ((component.Factions & hostile) == 0)
                    continue;

                if (!xformQuery.TryGetComponent(component.Owner, out var targetXform))
                    continue;

                if (targetXform.MapID != xform.MapID)
                    continue;

                if (!targetXform.Coordinates.InRange(EntityManager, xform.Coordinates, range))
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
        Dragon = 1 << 0,
        NanoTrasen = 1 << 1,
        SimpleHostile = 1 << 2,
        SimpleNeutral = 1 << 3,
        Syndicate = 1 << 4,
        Xeno = 1 << 5,
    }
}
