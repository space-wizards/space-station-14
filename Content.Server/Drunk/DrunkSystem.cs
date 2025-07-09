using Content.Server.Objectives.Components;
using Content.Shared.Drunk;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.StatusEffect;

namespace Content.Server.Drunk;

public sealed class DrunkSystem : SharedDrunkSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public const float DrunkInBarRange = 1.5f;

    private EntityQuery<DrunkInBarTargetComponent> _drunkInBarTargetQuery;

    public override void Initialize()
    {
        base.Initialize();

        _drunkInBarTargetQuery = GetEntityQuery<DrunkInBarTargetComponent>();
    }

    public override void TryApplyDrunkenness(EntityUid uid,
        float boozePower,
        bool applySlur = true,
        StatusEffectsComponent? status = null)
    {
        if (_mind.TryGetObjectiveComp<DrunkInBarConditionComponent>(uid, out var objective))
        {
            if (!objective.Completed)
            {
                foreach (var nearbyEntity in _entityLookup.GetEntitiesInRange(uid, objective.Range))
                {
                    if (_drunkInBarTargetQuery.HasComponent(nearbyEntity))
                        objective.Completed = true;
                }
            }
        }
    }
}
