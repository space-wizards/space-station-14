using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Client.Animus.Conditions;

namespace Content.Client.Animus.Conditions;

public sealed partial class AnimusConditionHasMobState : AnimusConditionBase
{
    [DataField]
    public MobState State = MobState.Invalid;

    private MobStateSystem _mobStateSystem = null!;

    public override void Initialize(EntityManager entityManager)
    {
        base.Initialize(entityManager);
        _mobStateSystem = entityManager.System<MobStateSystem>();
    }

    protected override bool Evaluate(EntityUid entity)
    {
        return _mobStateSystem.HasState(entity, State);
    }
}
