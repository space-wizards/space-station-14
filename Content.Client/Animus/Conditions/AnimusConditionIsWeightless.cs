using Content.Client.Gravity;
using Robust.Client.Animus.Conditions;

namespace Content.Client.Animus.Conditions;

public sealed partial class AnimusConditionIsWeightless : AnimusConditionBase
{
    private GravitySystem _gravitySystem = null!;

    public override void Initialize(IEntityManager entityManager)
    {
        base.Initialize(entityManager);

        _gravitySystem = entityManager.System<GravitySystem>();
    }

    protected override bool Evaluate(EntityUid entity)
    {
        return _gravitySystem.IsWeightless(entity);
    }
}
