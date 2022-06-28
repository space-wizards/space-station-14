using Content.Server.Weapon.Melee;
using Content.Shared.StatusEffect;
using Content.Server.Stunnable;
using Content.Shared.Stunnable;
using Content.Shared.Inventory.Events;
using Content.Server.Weapon.Melee.Components;
using Content.Server.Clothing.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Containers;

namespace Content.Server.Abilities.Boxer
{
    public sealed class BoxingSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BoxerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<BoxerComponent, MeleeHitEvent>(ApplyBoxerModifiers);
            SubscribeLocalEvent<BoxingGlovesComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<BoxingGlovesComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<BoxingGlovesComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnInit(EntityUid uid, BoxerComponent boxer, ComponentInit args)
        {
            var meleeComp = EnsureComp<MeleeWeaponComponent>(uid);
            meleeComp.Range *= boxer.RangeBonus;
        }
        private void OnMeleeHit(EntityUid uid, BoxingGlovesComponent component, MeleeHitEvent args)
        {
            _containerSystem.TryGetContainingContainer(uid, out var equipee);
            TryComp<BoxerComponent>(equipee?.Owner, out var boxer);

            if (boxer != null)
            {
                Box(args.HitEntities, boxer.ParalyzeTime, boxer.ParalyzeChanceNoSlowdown, boxer.SlowdownTime, boxer.ParalyzeChanceWithSlowdown);
                return;
            }
            Box(args.HitEntities, modifier: 0.3f);
        }

        private void ApplyBoxerModifiers(EntityUid uid, BoxerComponent component, MeleeHitEvent args)
        {
            args.ModifiersList.Add(component.UnarmedModifiers);
        }

        private void OnEquipped(EntityUid uid, BoxingGlovesComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.SlotFlags.HasFlag(args.SlotFlags))
                return;

            // Set the component to active to the unequip check isn't CBT
            component.IsActive = true;

            EnsureComp<BoxingComponent>(args.Equipee);
        }

        private void OnUnequipped(EntityUid uid, BoxingGlovesComponent component, GotUnequippedEvent args)
        {
            // Only undo the resistance if it was affecting the user
            if (!component.IsActive)
                return;
            component.IsActive = false;
            RemComp<BoxingComponent>(args.Equipee);
        }


        private void Box(IEnumerable<EntityUid> hitEntities, float paralyzeTime = 5f, float paralyzeChanceNoSlowdown = 0.2f,
            float slowdownTime = 3f, float paralyzeChanceWithSlowdown = 0.5f, float modifier = 1f)
        {
            foreach (var entity in hitEntities)
            {
                if (!TryComp<StatusEffectsComponent>(entity, out var status))
                    continue;

                if (HasComp<KnockedDownComponent>(entity))
                    continue;

                if (!HasComp<SlowedDownComponent>(entity))
                {
                    if (_robustRandom.Prob(paralyzeChanceNoSlowdown * modifier))
                    {
                        SoundSystem.Play("/Audio/Weapons/boxingbell.ogg", Filter.Pvs(entity), entity);
                        _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(paralyzeTime * modifier), true, status);
                    }
                    else
                        _stunSystem.TrySlowdown(entity, TimeSpan.FromSeconds(slowdownTime * modifier), true,  0.5f, 0.5f, status);
                }
                else
                {
                    if (_robustRandom.Prob(paralyzeChanceWithSlowdown * modifier))
                    {
                        SoundSystem.Play("/Audio/Weapons/boxingbell.ogg", Filter.Pvs(entity),  entity);
                        _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(paralyzeTime * modifier), true, status);
                    }
                    else
                        _stunSystem.TrySlowdown(entity, TimeSpan.FromSeconds(slowdownTime * modifier), true,  0.5f, 0.5f, status);
                }
            }
        }
    }
}
