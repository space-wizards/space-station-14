using Content.Server.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems;

public sealed class VitalPartSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
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
        // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Bloodloss"), 300);
        _damageableSystem.TryChangeDamage(uid, damage);
    }
}
