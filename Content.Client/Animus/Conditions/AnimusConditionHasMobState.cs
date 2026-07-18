using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Client.Animus.Conditions;

namespace Content.Client.Animus.Conditions;

public sealed partial class AnimusConditionHasMobState : AnimusConditionBase
{
    /// <summary>
    /// The required <see cref="MobState"/> to proceed.
    /// </summary>
    [DataField]
    public MobState State = MobState.Invalid;

    private MobStateSystem _mobStateSystem = null!;

    public override void Initialize(IEntityManager entityManager)
    {
        base.Initialize(entityManager);
        _mobStateSystem = entityManager.System<MobStateSystem>();
    }

    protected override bool Evaluate(EntityUid entity)
    {
        return _mobStateSystem.HasState(entity, State);
    }
}
