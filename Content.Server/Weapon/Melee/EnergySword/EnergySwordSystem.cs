using Content.Server.Tools.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Weapon.Melee.EnergySword
{
    internal class EnergySwordSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blockerSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
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
            args.HitSoundOverride = comp.HitSound;
        }

        private void OnUseInHand(EntityUid uid, EnergySwordComponent comp, UseInHandEvent args)
        {
            if (args.Handled) return;

            if (!_blockerSystem.CanUse(args.User))
                return;

            args.Handled = true;

            if (comp.Activated)
            {
                TurnOff(comp);
            }
            else
            {
                TurnOn(comp);
            }
        }

        private void TurnOff(EnergySwordComponent comp)
        {
            if (!comp.Activated)
                return;

            if (TryComp(comp.Owner, out SharedItemComponent? item))
            {
                item.Size = 5;
            }

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.DeActivateSound.GetSound(), comp.Owner);

            comp.Activated = false;
            UpdateAppearance(comp, item);
        }

        private void TurnOn(EnergySwordComponent comp)
        {
            if (comp.Activated)
                return;

            if (TryComp(comp.Owner, out SharedItemComponent? item))
            {
                item.Size = 9999;
            }

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.ActivateSound.GetSound(), comp.Owner);

            comp.Activated = true;
            UpdateAppearance(comp, item);
        }

        private void UpdateAppearance(EnergySwordComponent component, SharedItemComponent? itemComponent = null)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent)) return;

            appearanceComponent.SetData(EnergySwordVisuals.Color, component.BladeColor);

            var status = component.Activated ? EnergySwordStatus.On : EnergySwordStatus.Off;
            if (component.Hacked)
                status |= EnergySwordStatus.Hacked;

            appearanceComponent.SetData(EnergySwordVisuals.State, status);
            // wew itemcomp
            if (Resolve(component.Owner, ref itemComponent, false))
            {
                itemComponent.EquippedPrefix = component.Activated ? "on" : "off";
                itemComponent.Color = component.BladeColor;
            }
        }

        private void OnInteractUsing(EntityUid uid, EnergySwordComponent comp, InteractUsingEvent args)
        {
            if (args.Handled) return;

            if (comp.Hacked || !_blockerSystem.CanInteract(args.User))
                return;

            if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.ContainsAny("Pulsing")) return;

            args.Handled = true;
            comp.Hacked = true;
            UpdateAppearance(comp);
        }
    }
}
