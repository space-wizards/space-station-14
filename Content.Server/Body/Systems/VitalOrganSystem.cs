using Content.Server.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems;

public sealed class VitalOrganSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VitalOrganComponent, MechanismRemovedFromBodyEvent>(OnMechanismRemovedFromBody);
    }

    private void OnMechanismRemovedFromBody(EntityUid uid, VitalOrganComponent component, MechanismRemovedFromBodyEvent args)
    {
        // TODO BODY SYSTEM KILL : Find a more elegant way of killing em than just dumping bloodloss damage.
        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Bloodloss"), 300);
        _damageableSystem.TryChangeDamage(args.Body, damage);
    }
}
