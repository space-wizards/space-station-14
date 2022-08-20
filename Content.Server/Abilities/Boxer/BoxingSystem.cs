using Content.Server.Weapon.Melee;
using Content.Server.Stunnable;
using Content.Shared.Inventory.Events;
using Content.Server.Weapon.Melee.Components;
using Content.Server.Clothing.Components;
using Content.Server.Damage.Components;
using Content.Server.Damage.Events;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
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
            SubscribeLocalEvent<BoxerComponent, MeleeHitEvent>(ApplyBoxerModifiers);
            SubscribeLocalEvent<BoxingGlovesComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnInit(EntityUid uid, BoxerComponent boxer, ComponentInit args)
        {
            if (TryComp<MeleeWeaponComponent>(uid, out var meleeComp))
                meleeComp.Range *= boxer.RangeBonus;
        }
        private void ApplyBoxerModifiers(EntityUid uid, BoxerComponent component, MeleeHitEvent args)
        {
            if (component.UnarmedModifiers == default!)
            {
                Logger.Warning("BoxerComponent on " + uid + " couldn't get damage modifiers. Know that adding components with damage modifiers through VV or similar is unsupported.");
                return;
            }

            args.ModifiersList.Add(component.UnarmedModifiers);
        }
        private void OnStamHit(EntityUid uid, BoxingGlovesComponent component, StaminaMeleeHitEvent args)
        {
            _containerSystem.TryGetContainingContainer(uid, out var equipee);
            if (TryComp<BoxerComponent>(equipee?.Owner, out var boxer))
                args.Multiplier *= boxer.BoxingGlovesModifier;
        }
    }
}
