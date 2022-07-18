using Content.Server.Fluids.Components;

namespace Content.Server.AI.EntitySystems
{
    public sealed class GoToPuddleSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;

        public EntityUid GetNearbyPuddle(EntityUid cleanbot, float range = 10)
        {
            foreach (var entity in _lookup.GetEntitiesInRange(cleanbot, range))
            {
                if (HasComp<PuddleComponent>(entity))
                    return entity;
            }

            return default;
        }
    }
}
