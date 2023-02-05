using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee;

namespace Content.Server.Abilities.Boxer
{
    public sealed class BoxingSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BoxingGlovesComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<BoxingGlovesComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnInit(EntityUid uid, BoxingGlovesComponent component, ComponentInit args)
        {
            if (TryComp<MeleeWeaponComponent>(uid, out var meleeComp))
                meleeComp.Range *= component.RangeModifier;
        }

        private void OnStamHit(EntityUid uid, BoxingGlovesComponent component, StaminaMeleeHitEvent args)
        {
            args.Multiplier *= component.StamDamageModifier;
        }
    }
}
