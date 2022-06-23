using Content.Server.Weapon.Melee;
using Content.Shared.StatusEffect;
using Content.Shared.Sound;
using Content.Server.Stunnable;
using Content.Shared.Stunnable;
using Content.Shared.Inventory.Events;
using Content.Server.Weapon.Melee.Components;
using Content.Server.Clothing.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Abilities.Boxer
{
    public sealed class BoxingSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BoxerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<BoxingComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<BoxerComponent, MeleeHitEvent>(ApplyBoxerModifiers);
            SubscribeLocalEvent<BoxingGlovesComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<BoxingGlovesComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnInit(EntityUid uid, BoxerComponent boxer, ComponentInit args)
        {
            var meleeComp = EnsureComp<MeleeWeaponComponent>(uid);
            meleeComp.Range *= boxer.RangeBonus;
        }
        private void OnMeleeHit(EntityUid uid, BoxingComponent component, MeleeHitEvent args)
        {
            float boxModifier = 1f;
            if (!TryComp<BoxerComponent>(uid, out var boxer) || !boxer.Enabled)
                boxModifier *= 0.3f;

            if (boxer != null)
            {
                Box(args.HitEntities, boxer.ParalyzeTime, boxer.ParalyzeChanceNoSlowdown, boxer.SlowdownTime, boxer.ParalyzeChanceWithSlowdown, boxModifier);
                return;
            }
            Box(args.HitEntities, modifier: boxModifier);
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
            if (TryComp<BoxerComponent>(args.Equipee, out var boxer))
                boxer.Enabled = true;

            // Set the component to active to the unequip check isn't CBT
            component.IsActive = true;

            EnsureComp<BoxingComponent>(args.Equipee);

            if (TryComp<MeleeWeaponComponent>(args.Equipee, out var meleeComponent))
            {
                meleeComponent.HitSound = component.HitSound;
                component.OldDamage = meleeComponent.Damage;
                meleeComponent.Damage = component.Damage;
            }
        }

        private void OnUnequipped(EntityUid uid, BoxingGlovesComponent component, GotUnequippedEvent args)
        {
            // Only undo the resistance if it was affecting the user
            if (!component.IsActive)
                return;
            if(TryComp<BoxerComponent>(args.Equipee, out var boxer))
                boxer.Enabled = false;
            if (TryComp<MeleeWeaponComponent>(args.Equipee, out var meleeComponent))
            {
                meleeComponent.HitSound = new SoundCollectionSpecifier("GenericHit");
                meleeComponent.Damage = component.OldDamage;
                component.OldDamage = default!;
            }
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
                    return;

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
