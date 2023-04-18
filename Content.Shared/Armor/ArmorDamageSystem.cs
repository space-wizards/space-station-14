using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Armor;

namespace Content.Shared.ArmorDamage
{
    public sealed class ArmorSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        }
        private void OnDamageModify(EntityUid uid, ArmorComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
        {
            args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, component.Modifiers);
        }
    }
}
