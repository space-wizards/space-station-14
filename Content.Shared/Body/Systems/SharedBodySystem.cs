using Content.Shared.CharacterAppearance.Systems;
using Content.Shared.Damage;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBodySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeNetworking();
        InitializeManagerial();
    }

    [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] protected readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] protected readonly StandingStateSystem _standingStateSystem = default!;
    [Dependency] protected readonly SharedHumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] protected readonly DamageableSystem _damageableSystem = default!;
}
