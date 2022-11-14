using Content.Server.CombatMode.Disarm;
using Content.Server.Kitchen.Components;
using Content.Server.Weapons.Melee.EnergySword.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Content.Shared.Temperature;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Weapons.Melee.EnergySword
{
    public sealed class EnergySwordSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedRgbLightControllerSystem _rgbSystem = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EnergySwordComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<EnergySwordComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<EnergySwordComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<EnergySwordComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<EnergySwordComponent, IsHotEvent>(OnIsHotEvent);
        }

        private void OnMapInit(EntityUid uid, EnergySwordComponent comp, MapInitEvent args)
        {
            if (comp.ColorOptions.Count != 0)
                comp.BladeColor = _random.Pick(comp.ColorOptions);
        }

        private void OnMeleeHit(EntityUid uid, EnergySwordComponent comp, MeleeHitEvent args)
        {
            if (!comp.Activated) return;

            // Overrides basic blunt damage with burn+slash as set in yaml
            args.BonusDamage = comp.LitDamageBonus;
        }

        private void OnUseInHand(EntityUid uid, EnergySwordComponent comp, UseInHandEvent args)
        {
            if (args.Handled) return;

            args.Handled = true;

            if (comp.Activated)
            {
                TurnOff(comp);
            }
            else
            {
                TurnOn(comp);
            }

            UpdateAppearance(comp);
        }

        private void TurnOff(EnergySwordComponent comp)
        {
            if (!comp.Activated)
                return;

            if (TryComp(comp.Owner, out ItemComponent? item))
            {
                _item.SetSize(comp.Owner, 5, item);
            }

            if (TryComp<DisarmMalusComponent>(comp.Owner, out var malus))
            {
                malus.Malus -= comp.litDisarmMalus;
            }

            if(TryComp<MeleeWeaponComponent>(comp.Owner, out var weaponComp))
            {
                weaponComp.HitSound = comp.OnHitOff;
                if (comp.Secret)
                    weaponComp.HideFromExamine = true;
            }

            if (comp.IsSharp)
                RemComp<SharpComponent>(comp.Owner);

            SoundSystem.Play(comp.DeActivateSound.GetSound(), Filter.Pvs(comp.Owner, entityManager: EntityManager), comp.Owner);

            comp.Activated = false;
        }

        private void TurnOn(EnergySwordComponent comp)
        {
            if (comp.Activated)
                return;

            if (TryComp(comp.Owner, out ItemComponent? item))
            {
                _item.SetSize(comp.Owner, 9999, item);
            }

            if (comp.IsSharp)
                EnsureComp<SharpComponent>(comp.Owner);

            if(TryComp<MeleeWeaponComponent>(comp.Owner, out var weaponComp))
            {
                weaponComp.HitSound = comp.OnHitOn;
                if (comp.Secret)
                    weaponComp.HideFromExamine = false;
            }

            SoundSystem.Play(comp.ActivateSound.GetSound(), Filter.Pvs(comp.Owner, entityManager: EntityManager), comp.Owner);

            if (TryComp<DisarmMalusComponent>(comp.Owner, out var malus))
            {
                malus.Malus += comp.litDisarmMalus;
            }

            comp.Activated = true;
        }

        private void UpdateAppearance(EnergySwordComponent component)
        {
            if (!TryComp(component.Owner, out AppearanceComponent? appearanceComponent))
                return;

            appearanceComponent.SetData(ToggleableLightVisuals.Enabled, component.Activated);
            appearanceComponent.SetData(ToggleableLightVisuals.Color, component.BladeColor);
        }

        private void OnInteractUsing(EntityUid uid, EnergySwordComponent comp, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.ContainsAny("Pulsing")) return;

            args.Handled = true;
            comp.Hacked = !comp.Hacked;

            if (comp.Hacked)
            {
                var rgb = EnsureComp<RgbLightControllerComponent>(uid);
                _rgbSystem.SetCycleRate(uid, comp.CycleRate, rgb);
            }
            else
                RemComp<RgbLightControllerComponent>(uid);
        }
        private void OnIsHotEvent(EntityUid uid, EnergySwordComponent energySword, IsHotEvent args)
        {
            args.IsHot = energySword.Activated;
        }
    }
}
