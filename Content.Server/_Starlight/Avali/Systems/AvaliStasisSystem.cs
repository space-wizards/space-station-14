using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Starlight.Avali.Systems;
using Content.Shared.Starlight.Avali.Components;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.FixedPoint;

namespace Content.Server.Starlight.Avali.Systems;

public sealed class AvaliStasisSystem : SharedAvaliStasisSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AvaliStasisComponent, DamageChangedEvent>(OnDamageChanged);
    }

    protected override void OnEnterStasisStart(EntityUid uid, AvaliStasisComponent comp, AvaliEnterStasisActionEvent args)
    {
        base.OnEnterStasisStart(uid, comp, args);

        // Remove bleeding when entering stasis
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            _bloodstreamSystem.TryModifyBleedAmount(uid, -bloodstream.BleedAmount, bloodstream);
        }
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
        healAmount.DamageDict.Add("Piercing", FixedPoint2.New(comp.StasisPiercingHealPerSecond * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Heat", FixedPoint2.New(comp.StasisHeatHealPerSecond * args.DeltaSeconds * -1));
        healAmount.DamageDict.Add("Cold", FixedPoint2.New(comp.StasisColdHealPerSecond * args.DeltaSeconds * -1));

        if (TryComp<DamageableComponent>(uid, out _))
        {
            _damageableSystem.TryChangeDamage(uid, healAmount, true, origin: uid);
        }

        // Heal bleeding
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream) && bloodstream.BleedAmount > 0)
        {
            _bloodstreamSystem.TryModifyBleedAmount(uid, -comp.BleedHealPerSecond * (float)args.DeltaSeconds, bloodstream);
        }
    }
}
