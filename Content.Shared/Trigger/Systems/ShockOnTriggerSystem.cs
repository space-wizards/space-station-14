using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Electrocution;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Trigger.Systems;

public sealed partial class ShockOnTriggerSystem : XOnTriggerSystem<ShockOnTriggerComponent>
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<DamageTypePrototype> ShockDamage = "Shock";

    protected override void OnTrigger(Entity<ShockOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // Override the normal target if we target the container
        if (ent.Comp.TargetContainer)
        {
            // shock whoever is wearing this clothing item
            if (!_container.TryGetContainingContainer(ent.Owner, out var container))
                return;

            target = container.Owner;
        }

        DamageSpecifier damage = new(_prototypes.Index(ShockDamage), ent.Comp.Damage);
        _electrocution.TryDoElectrocution(target, null, damage, ent.Comp.Duration, true, ignoreInsulation: true);
        args.Handled = true;
    }
}
