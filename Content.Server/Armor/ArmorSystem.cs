using Content.Shared.Damage;
using Robust.Shared.GameObjects;

namespace Content.Server.Armor
{
    public sealed class ArmorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ArmorComponent, DamageModifyEvent>(OnDamageModify);
        }

        private void OnDamageModify(EntityUid uid, ArmorComponent component, DamageModifyEvent args)
        {
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, component.Modifiers);
        }
    }
}
