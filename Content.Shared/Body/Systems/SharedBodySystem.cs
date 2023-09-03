using Content.Shared.Damage;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBodySystem : EntitySystem
{
    protected const string BodyContainerId = "BodyContainer";

    [Dependency] protected IPrototypeManager Prototypes = default!;

    [Dependency] protected SharedContainerSystem Containers = default!;
    [Dependency] protected DamageableSystem Damageable = default!;
    [Dependency] protected StandingStateSystem Standing = default!;
    [Dependency] protected MovementSpeedModifierSystem Movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeBody();
        InitializeParts();
        InitializeOrgans();
    }
}
