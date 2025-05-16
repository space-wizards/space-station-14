using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.FixedPoint;
using Content.Shared.Starlight.Avali.Components;
using Content.Shared.Starlight.Avali.Events;
using Content.Shared.Starlight.Avali.Systems;

namespace Content.Server._Starlight.Avali.Systems;

public sealed class AvaliStasisSystem : SharedAvaliStasisSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AvaliStasisComponent, DamageChangedEvent>(OnDamageChanged);
    }

    protected override void OnMapInit(EntityUid uid, AvaliStasisComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
    }

    /// <summary>
    /// Takeths away the action to preform stasis from the entity.
    /// </summary>
    protected override void OnCompRemove(EntityUid uid, AvaliStasisComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
    }

    protected override void OnEnterStasisStart(EntityUid uid, AvaliStasisComponent comp,
        AvaliEnterStasisActionEvent args)
    {
        comp.IsInStasis = true;
        Dirty(uid, comp);
        
        EnsureComp<AvaliStasisFrozenComponent>(uid);

        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.ExitStasisActionEntity, comp.ExitStasisAction);

        // Remove bleeding when entering stasis
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            _bloodstreamSystem.TryModifyBleedAmount(uid, -bloodstream.BleedAmount, bloodstream);
        }

        // Send animation event to all clients
        var ev = new AvaliStasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates), AvaliStasisAnimationType.Enter);
        RaiseNetworkEvent(ev);
    }

    protected override void OnExitStasisStart(EntityUid uid, AvaliStasisComponent comp, AvaliExitStasisActionEvent args)
    {
        comp.IsInStasis = false;
        Dirty(uid, comp);
        
        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
        _actionsSystem.SetCooldown(comp.EnterStasisActionEntity, TimeSpan.FromSeconds(comp.StasisCooldown));

        RemComp<AvaliStasisFrozenComponent>(uid);

        // Send animation event to all clients
        var ev = new AvaliStasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates), AvaliStasisAnimationType.Exit);
        RaiseNetworkEvent(ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AvaliStasisComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            OnStasisUpdate(uid, comp, new FrameEventArgs(frameTime));
        }
    }

    private void OnDamageChanged(EntityUid uid, AvaliStasisComponent component, DamageChangedEvent args)
    {
        // Skip if this is healing or if the damage change is from our own healing
        if (!args.DamageIncreased || args.DamageDelta == null || args.Origin == uid)
            return;

        // Only apply resistance if in stasis
        if (!component.IsInStasis)
            return;

        // Skip if this damage was already modified by stasis
        if (args.Origin == uid && args.DamageDelta.DamageDict.All(x => x.Value < 0))
            return;

        // Create new DamageSpecifier with reduced damage
        var damageToApply = new DamageSpecifier();
        foreach (var (type, amount) in args.DamageDelta.DamageDict)
        {
            damageToApply.DamageDict.Add(type, amount - amount * component.StasisAdditionalDamageResistance);
        }

        // Apply the reduced damage
        _damageableSystem.TryChangeDamage(uid, damageToApply, true, origin: uid);
    }

    private void OnStasisUpdate(EntityUid uid, AvaliStasisComponent comp, FrameEventArgs args)
    {
        if (!comp.IsInStasis)
            return;

        // Apply healing effect
        var healAmount = new DamageSpecifier();
        healAmount.DamageDict.Add("Blunt", FixedPoint2.New(comp.StasisBluntHealPerSecond * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Slash", FixedPoint2.New(comp.StasisSlashingHealPerSecond * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Piercing",
            FixedPoint2.New(comp.StasisPiercingHealPerSecond * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Heat", FixedPoint2.New(comp.StasisHeatHealPerSecond * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Cold", FixedPoint2.New(comp.StasisColdHealPerSecond * args.DeltaSeconds * -1));

        if (TryComp<DamageableComponent>(uid, out _))
        {
            _damageableSystem.TryChangeDamage(uid, healAmount, true, origin: uid);
        }

        // Heal bleeding
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream) && bloodstream.BleedAmount > 0)
        {
            _bloodstreamSystem.TryModifyBleedAmount(uid, -comp.BleedHealPerSecond * (float)args.DeltaSeconds,
                bloodstream);
        }
    }
}