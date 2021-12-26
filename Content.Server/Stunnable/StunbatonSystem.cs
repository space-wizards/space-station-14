using System;
using System.Linq;
using Content.Server.Items;
using Content.Server.PowerCell.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Stunnable.Components;
using Content.Server.Weapon.Melee;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Stunnable
{
    public class StunbatonSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly StutteringSystem _stutteringSystem = default!;
        [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<StunbatonComponent, MeleeInteractEvent>(OnMeleeInteract);
            SubscribeLocalEvent<StunbatonComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<StunbatonComponent, ThrowDoHitEvent>(OnThrowCollide);
            SubscribeLocalEvent<StunbatonComponent, PowerCellChangedEvent>(OnPowerCellChanged);
            SubscribeLocalEvent<StunbatonComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
        }

        private void OnMeleeHit(EntityUid uid, StunbatonComponent comp, MeleeHitEvent args)
        {
            if (!comp.Activated || !args.HitEntities.Any())
                return;

            if (!EntityManager.TryGetComponent<PowerCellSlotComponent>(uid, out var slot) || slot.Cell == null || !slot.Cell.TryUseCharge(comp.EnergyPerUse))
                return;

            foreach (EntityUid entity in args.HitEntities)
            {
                StunEntity(entity, comp);
            }
        }

        private void OnMeleeInteract(EntityUid uid, StunbatonComponent comp, MeleeInteractEvent args)
        {
            if (!comp.Activated)
                return;

            if (!EntityManager.TryGetComponent<PowerCellSlotComponent>(uid, out var slot) || slot.Cell == null || !slot.Cell.TryUseCharge(comp.EnergyPerUse))
                return;

            args.CanInteract = true;
            StunEntity(args.Entity, comp);
        }

        private void OnUseInHand(EntityUid uid, StunbatonComponent comp, UseInHandEvent args)
        {
            if (!Get<ActionBlockerSystem>().CanUse(args.User))
                return;

            if (comp.Activated)
            {
                TurnOff(comp);
            }
            else
            {
                TurnOn(comp, args.User);
            }
        }

        private void OnThrowCollide(EntityUid uid, StunbatonComponent comp, ThrowDoHitEvent args)
        {
            if (!EntityManager.TryGetComponent<PowerCellSlotComponent>(uid, out var slot)) return;
            if (!comp.Activated || slot.Cell == null || !slot.Cell.TryUseCharge(comp.EnergyPerUse)) return;

            StunEntity(args.Target, comp);
        }

        private void OnPowerCellChanged(EntityUid uid, StunbatonComponent comp, PowerCellChangedEvent args)
        {
            if (args.Ejected)
            {
                TurnOff(comp);
            }
        }

        private void OnInteractUsing(EntityUid uid, StunbatonComponent comp, InteractUsingEvent args)
        {
            if (!Get<ActionBlockerSystem>().CanInteract(args.User))
                return;

            if (EntityManager.TryGetComponent<PowerCellSlotComponent>(uid, out var cellslot))
                cellslot.InsertCell(args.Used);
        }

        private void OnExamined(EntityUid uid, StunbatonComponent comp, ExaminedEvent args)
        {
            var msg = comp.Activated
                ? Loc.GetString("comp-stunbaton-examined-on")
                : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(msg);
        }

        private void StunEntity(EntityUid entity, StunbatonComponent comp)
        {
            if (!EntityManager.TryGetComponent(entity, out StatusEffectsComponent? status) || !comp.Activated) return;

            // TODO: Make slowdown inflicted customizable.

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.StunSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));
            if (!EntityManager.HasComponent<SlowedDownComponent>(entity))
            {
                if (_robustRandom.Prob(comp.ParalyzeChanceNoSlowdown))
                    _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(comp.ParalyzeTime), true, status);
                else
                    _stunSystem.TrySlowdown(entity, TimeSpan.FromSeconds(comp.SlowdownTime), true,  0.5f, 0.5f, status);
            }
            else
            {
                if (_robustRandom.Prob(comp.ParalyzeChanceWithSlowdown))
                    _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(comp.ParalyzeTime), true, status);
                else
                    _stunSystem.TrySlowdown(entity, TimeSpan.FromSeconds(comp.SlowdownTime), true,  0.5f, 0.5f, status);
            }

            var slowdownTime = TimeSpan.FromSeconds(comp.SlowdownTime);
            _jitterSystem.DoJitter(entity, slowdownTime, true, status:status);
            _stutteringSystem.DoStutter(entity, slowdownTime, true, status);

            if (!EntityManager.TryGetComponent<PowerCellSlotComponent?>(comp.Owner, out var slot) || slot.Cell == null || !(slot.Cell.CurrentCharge < comp.EnergyPerUse))
                return;

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.SparksSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));
            TurnOff(comp);
        }

        private void TurnOff(StunbatonComponent comp)
        {
            if (!comp.Activated)
            {
                return;
            }

            if (!EntityManager.TryGetComponent<SpriteComponent?>(comp.Owner, out var sprite) ||
                !EntityManager.TryGetComponent<ItemComponent?>(comp.Owner, out var item)) return;

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.SparksSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));
            item.EquippedPrefix = "off";
            // TODO stunbaton visualizer
            sprite.LayerSetState(0, "stunbaton_off");
            comp.Activated = false;
        }

        private void TurnOn(StunbatonComponent comp, EntityUid user)
        {
            if (comp.Activated)
            {
                return;
            }

            if (!EntityManager.TryGetComponent<SpriteComponent?>(comp.Owner, out var sprite) ||
                !EntityManager.TryGetComponent<ItemComponent?>(comp.Owner, out var item))
                return;

            var playerFilter = Filter.Pvs(comp.Owner);
            if (!EntityManager.TryGetComponent<PowerCellSlotComponent?>(comp.Owner, out var slot))
                return;

            if (slot.Cell == null)
            {
                SoundSystem.Play(playerFilter, comp.TurnOnFailSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("comp-stunbaton-activated-missing-cell"));
                return;
            }

            if (slot.Cell != null && slot.Cell.CurrentCharge < comp.EnergyPerUse)
            {
                SoundSystem.Play(playerFilter, comp.TurnOnFailSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("comp-stunbaton-activated-dead-cell"));
                return;
            }

            SoundSystem.Play(playerFilter, comp.SparksSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "stunbaton_on");
            comp.Activated = true;
        }
    }
}
