using Content.Shared.Item;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.ActionBlocker;
using Content.Shared.Weapons.Melee;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Log;

namespace Content.Server.Weapon.Melee.Esword
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
            if (!_blockerSystem.CanUse(args.User))
                return;

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

            UpdateAppearance(comp);
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

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.ActivateSound.GetSound(), comp.Owner);

            UpdateAppearance(comp);

            comp.Activated = true;
        }

        private void UpdateAppearance(EnergySwordComponent component)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent)) return;

            appearanceComponent.SetData(EnergySwordVisuals.Color, component.BladeColor);
            appearanceComponent.SetData(EnergySwordVisuals.Hacked, component.Hacked);
            appearanceComponent.SetData(EnergySwordVisuals.State, component.Activated);
        }

        private void OnInteractUsing(EntityUid uid, EnergySwordComponent comp, InteractUsingEvent args)
        {
            if (comp.Hacked || !_blockerSystem.CanInteract(args.User))
                return;

            if (!TryComp(args.Used, out ToolComponent? tool) || !tool.Qualities.ContainsAny("Pulsing")) return;

            comp.Hacked = true;

            if (!comp.Activated) return;

            UpdateAppearance(comp);
        }
    }
}
