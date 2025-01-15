using Content.Server.Examine;
using Content.Server.Traits.Assorted;
using Content.Shared._EinsteinEngines.ParacusiaProximity;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Traits.Assorted;

namespace Content.Server._EinsteinEngines.ParacusiaProximity;

public sealed partial class ParacusiaProximitySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = _entity.EntityQueryEnumerator<ParacusiaProximityComponent>();

        while (query.MoveNext(out var uid, out var proximity))
        {
            var coords = Transform(uid).Coordinates;
            var lookup = _lookup.GetEntitiesInRange<MobStateComponent>(coords, proximity.Range);

            foreach (var mob in lookup)
            {
                // Ignore silicons, paracusia proximity immunity, and occluded mobs
                if (HasComp<SiliconLawBoundComponent>(mob) ||
                    HasComp<ParacusiaProximityImmuneComponent>(mob) ||
                    !_examine.InRangeUnOccluded(uid, mob, proximity.Range))
                    continue;

                if (!EnsureComp<ParacusiaComponent>(mob, out var paracusia))
                {
                    _paracusia.SetSounds(mob, proximity.Sounds, paracusia);
                    _paracusia.SetTime(mob, proximity.MinTimeBetweenIncidents, proximity.MaxTimeBetweenIncidents, paracusia);
                    _paracusia.SetDistance(mob, proximity.MaxSoundDistance, paracusia);
                }
            }
        }
    }
}
