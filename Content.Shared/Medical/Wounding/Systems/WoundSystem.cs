using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Events;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly GibbingSystem _gibbingSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;


    private TimeSpan _healingUpdateRate = new TimeSpan(0,0,1);

    public override void Initialize()
    {
        SubscribeLocalEvent<WoundableComponent, ComponentInit>(OnWoundableInit);
        InitWounding();
    }

    public override void Update(float frameTime)
    {
        HealingUpdate(frameTime);
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

    public void SetWoundSeverity(Entity<WoundComponent?> wound, FixedPoint2 newSeverity)
    {
        if (!Resolve(wound, ref wound.Comp) || wound.Comp.Severity == newSeverity)
            return;
        if (newSeverity > 100)
        {
            Log.Warning($"Wound Severity must be within the range of 0-100. Tried to set severity of {newSeverity}, this will be clamped.");
            newSeverity = 100;
        }
        if (newSeverity < 0)
        {
            Log.Warning($"Wound Severity must be within the range of 0-100. Tried to set severity of {newSeverity}, this will be clamped.");
            newSeverity = 0;
        }

        var oldSev = wound.Comp.Severity;
        wound.Comp.Severity = newSeverity;
        var ev = new WoundSeverityChangedEvent(new Entity<WoundComponent>(wound, wound.Comp), wound.Comp.Severity-oldSev);
        RaiseRelayedLocalEvent(null, new Entity<WoundComponent>(wound, wound.Comp), ref ev);
        Dirty(wound, wound.Comp);
    }

    public void AddWoundSeverity(Entity<WoundComponent?> wound, FixedPoint2 severityAddition)
    {
        if (!Resolve(wound, ref wound.Comp) || severityAddition == 0)
            return;
        var oldSev = wound.Comp.Severity;
        wound.Comp.Severity = FixedPoint2.Clamp(wound.Comp.Severity+severityAddition, 0, 100);
        var ev = new WoundSeverityChangedEvent(new Entity<WoundComponent>(wound, wound.Comp), wound.Comp.Severity-oldSev);
        RaiseRelayedLocalEvent(null, new Entity<WoundComponent>(wound, wound.Comp), ref ev);
        Dirty(wound, wound.Comp);
    }

    public void SetWoundableHealth(Entity<WoundableComponent?> woundable, FixedPoint2 newHealth,
        ProtoId<DamageTypePrototype>? damageTypeOverride = null)
    {
        if (!Resolve(woundable, ref woundable.Comp) || woundable.Comp.Health == newHealth)
            return;
        var oldHealth = woundable.Comp.Health;
        woundable.Comp.Health = FixedPoint2.Clamp(newHealth, 0, woundable.Comp.HealthCap);

        if (damageTypeOverride != null)
            woundable.Comp.LastAppliedDamageType = damageTypeOverride.Value;
        var ev = new WoundableHealthChangedEvent(
            new Entity<WoundableComponent>(woundable, woundable.Comp),
            woundable.Comp.Health-oldHealth);
        RaiseRelayedLocalEvent(new Entity<WoundableComponent>(woundable, woundable.Comp), null,ref ev);
        if (newHealth < 0)
            AddWoundableIntegrity(woundable, newHealth);
        Dirty(woundable, woundable.Comp);
    }

    public void AddWoundableHealth(Entity<WoundableComponent?> woundable, FixedPoint2 healthAddition,
        ProtoId<DamageTypePrototype>? damageTypeOverride = null)
    {
        if (!Resolve(woundable, ref woundable.Comp) || healthAddition == 0)
            return;
        var oldHealth = woundable.Comp.Health;
        var newHealth = woundable.Comp.Health + healthAddition;
        woundable.Comp.Health = FixedPoint2.Clamp(newHealth, 0, woundable.Comp.HealthCap);
        if (damageTypeOverride != null)
            woundable.Comp.LastAppliedDamageType = damageTypeOverride.Value;
        var ev = new WoundableHealthChangedEvent(
            new Entity<WoundableComponent>(woundable, woundable.Comp),
            woundable.Comp.Health-oldHealth);
        RaiseRelayedLocalEvent(new Entity<WoundableComponent>(woundable, woundable.Comp), null,ref ev);

        if (newHealth < 0)
            AddWoundableIntegrity(woundable, newHealth);
        Dirty(woundable, woundable.Comp);
    }

    public void SetWoundableIntegrity(Entity<WoundableComponent?> woundable, FixedPoint2 newIntegrity,
        ProtoId<DamageTypePrototype>? damageTypeOverride = null)
    {
        if (!Resolve(woundable, ref woundable.Comp) || woundable.Comp.Integrity == newIntegrity)
            return;
        var oldInt = woundable.Comp.Integrity;
        woundable.Comp.Integrity = FixedPoint2.Clamp(newIntegrity, 0, woundable.Comp.IntegrityCap);

        if (damageTypeOverride != null)
            woundable.Comp.LastAppliedDamageType = damageTypeOverride.Value;
        var ev = new WoundableIntegrityChangedEvent(
            new Entity<WoundableComponent>(woundable, woundable.Comp),
            woundable.Comp.Integrity-oldInt);
        RaiseRelayedLocalEvent(new Entity<WoundableComponent>(woundable, woundable.Comp), null,ref ev);
        if (!CheckWoundableGibbing(woundable, woundable.Comp))
            Dirty(woundable, woundable.Comp);
    }

    public void AddWoundableIntegrity(Entity<WoundableComponent?> woundable, FixedPoint2 integrityAddition,
        ProtoId<DamageTypePrototype>? damageTypeOverride = null)
    {
        if (!Resolve(woundable, ref woundable.Comp) || integrityAddition == 0)
            return;
        var oldInt = woundable.Comp.Integrity;
        var newInt = woundable.Comp.Integrity + integrityAddition;

        if (damageTypeOverride != null)
            woundable.Comp.LastAppliedDamageType = damageTypeOverride.Value;
        woundable.Comp.Integrity = FixedPoint2.Clamp(newInt, 0, woundable.Comp.IntegrityCap);
        var ev = new WoundableIntegrityChangedEvent(
            new Entity<WoundableComponent>(woundable, woundable.Comp),
            woundable.Comp.Integrity-oldInt);
        RaiseRelayedLocalEvent(new Entity<WoundableComponent>(woundable, woundable.Comp), null,ref ev);
        if (!CheckWoundableGibbing(woundable, woundable.Comp))
            Dirty(woundable, woundable.Comp);
    }

    private void RaiseRelayedLocalEvent<T>(Entity<WoundableComponent>? woundable, Entity<WoundComponent>? wound, ref T woundEvent) where T : struct
    {
        if (wound != null)
            RaiseLocalEvent(wound.Value.Owner, ref woundEvent);
        if (woundable == null)
            return;
        RaiseLocalEvent(woundable.Value.Owner, ref woundEvent);
        if(woundable.Value.Comp!.Body != null)
            RaiseLocalEvent(woundable.Value.Comp.Body.Value, ref woundEvent);
    }
}
