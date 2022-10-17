using Content.Shared.Damage;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

[Virtual]
public abstract partial class SharedBodySystem : EntitySystem
{
    private const string BodyContainerId = "BodyContainer";

    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeBody();
        InitializeParts();
        InitializeOrgans();
    }
}
