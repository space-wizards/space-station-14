using Content.Server.Body.Components;
using Content.Server.MobState;
using Content.Shared.Body.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.MobState.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems;

public sealed class VitalPartSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VitalPartComponent, MechanismRemovedFromBodyEvent>(OnMechanismRemovedFromBody);
        SubscribeLocalEvent<VitalPartComponent, MechanismRemovedFromPartInBodyEvent>(OnMechanismRemovedFromPartInBody);
        SubscribeLocalEvent<VitalPartComponent, PartRemovedFromBodyEvent>(OnPartRemovedFromBody);
    }

    private void OnMechanismRemovedFromBody(EntityUid uid, VitalPartComponent component, MechanismRemovedFromBodyEvent args)
    {
        ApplyDamage(args.Body);
    }

    private void OnMechanismRemovedFromPartInBody(EntityUid uid, VitalPartComponent component, MechanismRemovedFromPartInBodyEvent args)
    {
        ApplyDamage(args.Body);
    }

    private void OnPartRemovedFromBody(EntityUid uid, VitalPartComponent component, PartRemovedFromBodyEvent args)
    {
        ApplyDamage(args.Body);
    }

    private void ApplyDamage(EntityUid uid)
    {
        // Don't need to bother applying damage if it has a mob state or is aleady dead since this is temporary solution for vital parts anyway
        if (!TryComp<MobStateComponent>(uid, out var mobState) ||
            _mobStateSystem.IsDead(uid, mobState))
            return;

        // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Bloodloss"), 300);
        _damageableSystem.TryChangeDamage(uid, damage);
    }
}
