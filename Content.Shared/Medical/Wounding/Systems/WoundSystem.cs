using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Medical.Wounding.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GibbingSystem _gibbingSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;


    private TimeSpan _healingUpdateRate = new TimeSpan(0,0,1);

    public override void Initialize()
    {
        SubscribeLocalEvent<WoundableComponent, ComponentInit>(OnWoundableInit);
        InitWounding();
        InitBodyListeners();
        InitDamage();
    }

    public override void Update(float frameTime)
    {
    }

    private void OnWoundableInit(EntityUid owner, WoundableComponent woundable, ref ComponentInit args)
    {
        woundable.HealthCap = woundable.MaxHealth;
        woundable.IntegrityCap = woundable.MaxIntegrity;
        if (woundable.Health <= 0)
            woundable.Health = woundable.HealthCap;
        if (woundable.Integrity <= 0)
            woundable.Integrity = woundable.IntegrityCap;
        _containerSystem.EnsureContainer<Container>(owner, WoundableComponent.WoundableContainerId);
    }
}
