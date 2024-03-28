using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Medical.Wounding.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
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
    [Dependency] private readonly INetManager _netManager = default!;


    private TimeSpan _healingUpdateRate = new TimeSpan(0,0,1);

    public override void Initialize()
    {
        if (!_netManager.IsClient)
            SubscribeLocalEvent<WoundableComponent, MapInitEvent>(WoundableInit);
        InitWounding();
        InitBodyListeners();
        InitDamage();
    }

    public override void Update(float frameTime)
    {
    }

    private void WoundableInit(EntityUid owner, WoundableComponent woundable, ref MapInitEvent args)
    {
        woundable.HealthCap = woundable.MaxHealth;
        woundable.IntegrityCap = woundable.MaxIntegrity;
        if (woundable.Health < 0)
            woundable.Health = woundable.HealthCap;
        if (woundable.Integrity <= 0)
            woundable.Integrity = woundable.IntegrityCap;
        _containerSystem.EnsureContainer<Container>(owner, WoundableComponent.WoundableContainerId);
        Dirty(owner,woundable);
    }

    public void ChangeIntegrity(Entity<WoundableComponent> woundable, FixedPoint2 deltaIntegrity)
    {
        woundable.Comp.Integrity += deltaIntegrity;
        ValidateWoundable(woundable);
    }

    public void ChangeIntegrityCap(Entity<WoundableComponent> woundable, FixedPoint2 deltaIntegrityCap)
    {
        woundable.Comp.IntegrityCap += deltaIntegrityCap;
        ValidateWoundable(woundable);
    }

    public void ChangeHealthCap(Entity<WoundableComponent> woundable, FixedPoint2 deltaHealthCap)
    {
        woundable.Comp.HealthCap += deltaHealthCap;
        ValidateWoundable(woundable);
    }

}
