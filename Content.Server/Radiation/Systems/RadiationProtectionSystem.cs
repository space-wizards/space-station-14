using Content.Server.Radiation.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Radiation.EntitySystems;

public sealed class RadiationProtectionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadiationProtectionComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, RadiationProtectionComponent component, DamageModifyEvent args)
    {
        // Maybe cache this for performance?
        if (!_prototypeManager.TryIndex<DamageModifierSetPrototype>(component.RadiationProtectionModifierSetId, out var modifier))
            return;
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
    }
}
