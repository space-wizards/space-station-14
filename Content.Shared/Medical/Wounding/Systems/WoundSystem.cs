using Content.Shared.Damage;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Medical.Wounding.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    private readonly SharedContainerSystem _containerSystem = default!;
    private readonly IPrototypeManager _prototypeManager = default!;
    private readonly GibbingSystem _gibbingSystem = default!;
    private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WoundableComponent, ComponentInit>(OnWoundableInit);
        InitWounding();
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
        Dirty(owner, woundable); //TODO: Is this dirty call needed? This should be run on both server and client so i don't think so?
    }




}
