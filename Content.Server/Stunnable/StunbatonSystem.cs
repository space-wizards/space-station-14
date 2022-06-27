using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Server.Speech.EntitySystems;
using Content.Server.Stunnable.Components;
using Content.Server.Weapon.Melee;
using Content.Server.Weapon.Melee.Components;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Stunnable
{
    public sealed class StunbatonSystem : EntitySystem
    {
        [Dependency] private readonly MeleeWeaponSystem _melee = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly StutteringSystem _stutteringSystem = default!;
        [Dependency] private readonly SharedJitteringSystem _jitterSystem = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<StunbatonComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<StunbatonComponent, ThrowDoHitEvent>(OnThrowCollide);
            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
        }

        private void OnMeleeHit(EntityUid uid, StunbatonComponent comp, MeleeHitEvent args)
        {
            if (!comp.Activated || !args.HitEntities.Any() || args.Handled || _useDelay.ActiveDelay(uid))
                return;

            if (!TryComp<BatteryComponent>(uid, out var battery) || !battery.TryUseCharge(comp.EnergyPerUse))
                return;

            foreach (var entity in args.HitEntities)
            {
                StunEntity(entity, comp);
                SendPowerPulse(entity, args.User, uid);
            }

            _melee.SetAttackCooldown(uid, _timing.CurTime + comp.ActiveDelay);
            _useDelay.BeginDelay(uid);
            // No combat should occur if we successfully stunned.
            args.Handled = true;
        }

        private void OnUseInHand(EntityUid uid, StunbatonComponent comp, UseInHandEvent args)
        {
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
            if (!comp.Activated)
                return;

            if (!TryComp<BatteryComponent>(uid, out var battery))
                return;

            if (_robustRandom.Prob(comp.OnThrowStunChance) && battery.TryUseCharge(comp.EnergyPerUse))
            {
                SendPowerPulse(args.Target, args.User, uid);
                StunEntity(args.Target, comp);
            }
        }

        private void OnExamined(EntityUid uid, StunbatonComponent comp, ExaminedEvent args)
        {
            var msg = comp.Activated
                ? Loc.GetString("comp-stunbaton-examined-on")
                : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(msg);
            if(TryComp<BatteryComponent>(uid, out var battery))
                args.PushMarkup(Loc.GetString("stunbaton-component-on-examine-charge",
                    ("charge", (int)((battery.CurrentCharge/battery.MaxCharge) * 100))));
        }

        private void StunEntity(EntityUid entity, StunbatonComponent comp)
        {
            if (!EntityManager.TryGetComponent(entity, out StatusEffectsComponent? status) || !comp.Activated) return;

            SoundSystem.Play(comp.StunSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner, AudioHelpers.WithVariation(0.25f));
            _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(comp.ParalyzeTime), true, status);

            var slowdownTime = TimeSpan.FromSeconds(comp.ParalyzeTime);
            _jitterSystem.DoJitter(entity, slowdownTime, true, status:status);
            _stutteringSystem.DoStutter(entity, slowdownTime, true, status);

            if (!TryComp<BatteryComponent>(comp.Owner, out var battery) || !(battery.CurrentCharge < comp.EnergyPerUse))
                return;

            SoundSystem.Play(comp.SparksSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner, AudioHelpers.WithVariation(0.25f));
            TurnOff(comp);
        }

        private void TurnOff(StunbatonComponent comp)
        {
            if (!comp.Activated)
                return;

            // TODO stunbaton visualizer
            if (TryComp<SpriteComponent>(comp.Owner, out var sprite) &&
                TryComp<SharedItemComponent>(comp.Owner, out var item))
            {
                item.EquippedPrefix = "off";
                sprite.LayerSetState(0, "stunbaton_off");
            }

            SoundSystem.Play(comp.SparksSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner, AudioHelpers.WithVariation(0.25f));

            comp.Activated = false;
            if (TryComp<UseDelayComponent>(comp.Owner, out var useDelay) && comp.OldDelay != null)
            {
                useDelay.Delay = comp.OldDelay.Value;
                comp.OldDelay = null;
            }
        }

        private void TurnOn(StunbatonComponent comp, EntityUid user)
        {
            if (comp.Activated)
                return;

            if (EntityManager.TryGetComponent<SpriteComponent?>(comp.Owner, out var sprite) &&
                EntityManager.TryGetComponent<SharedItemComponent?>(comp.Owner, out var item))
            {
                item.EquippedPrefix = "on";
                sprite.LayerSetState(0, "stunbaton_on");
            }

            var playerFilter = Filter.Pvs(comp.Owner, entityManager: EntityManager);
            if (!TryComp<BatteryComponent>(comp.Owner, out var battery) || battery.CurrentCharge < comp.EnergyPerUse)
            {
                SoundSystem.Play(comp.TurnOnFailSound.GetSound(), playerFilter, comp.Owner, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("stunbaton-component-low-charge"));
                return;
            }

            SoundSystem.Play(comp.SparksSound.GetSound(), playerFilter, comp.Owner, AudioHelpers.WithVariation(0.25f));

            comp.Activated = true;
            if (TryComp<UseDelayComponent>(comp.Owner, out var useDelay))
            {
                comp.OldDelay = useDelay.Delay;
                useDelay.Delay = comp.ActiveDelay;
            }
        }

        private void SendPowerPulse(EntityUid target, EntityUid? user, EntityUid used)
        {
            RaiseLocalEvent(target, new PowerPulseEvent()
            {
                Used = used,
                User = user
            }, false);
        }
    }
}
