using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using Content.Shared.Toggleable;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Weapon.Melee.EnergySword
{
    public sealed class EnergySwordSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedRgbLightControllerSystem _rgbSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EnergySwordComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<EnergySwordComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<EnergySwordComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<EnergySwordComponent, InteractUsingEvent>(OnInteractUsing);
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

            if (TryComp(comp.Owner, out SharedItemComponent? item))
            {
                item.Size = 5;
            }

            SoundSystem.Play(Filter.Pvs(comp.Owner, entityManager: EntityManager), comp.DeActivateSound.GetSound(), comp.Owner);

            comp.Activated = false;
        }

        private void TurnOn(EnergySwordComponent comp)
        {
            if (comp.Activated)
                return;

            if (TryComp(comp.Owner, out SharedItemComponent? item))
            {
                item.Size = 9999;
            }

            SoundSystem.Play(Filter.Pvs(comp.Owner, entityManager: EntityManager), comp.ActivateSound.GetSound(), comp.Owner);

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
    }
}
