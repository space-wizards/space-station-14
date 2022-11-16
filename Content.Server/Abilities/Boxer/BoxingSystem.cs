using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;

namespace Content.Server.Abilities.Boxer
{
    public sealed class BoxingSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BoxerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<BoxerComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<BoxingGlovesComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnInit(EntityUid uid, BoxerComponent component, ComponentInit args)
        {
            if (TryComp<MeleeWeaponComponent>(uid, out var meleeComp))
                meleeComp.Range *= component.RangeBonus;
        }
        private void OnMeleeHit(EntityUid uid, BoxerComponent component, MeleeHitEvent args)
        {
            args.ModifiersList.Add(component.UnarmedModifiers);
        }

        private void OnStamHit(EntityUid uid, BoxingGlovesComponent component, StaminaMeleeHitEvent args)
        {
            if (!_containerSystem.TryGetContainingContainer(uid, out var equipee))
                return;

            if (TryComp<BoxerComponent>(equipee.Owner, out var boxer))
                args.Multiplier *= boxer.BoxingGlovesModifier;
        }
    }
}
