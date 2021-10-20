using System;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Electrocution
{
    public abstract class SharedElectrocutionSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly SharedStutteringSystem _stutteringSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        protected const string StatusEffectKey = "Electrocution";
        protected const string DamageType = "Shock";

        private const float RecursiveDamageMultiplier = 0.75f;
        private const float RecursiveTimeMultiplier = 0.8f;

        private const float ParalyzeTimeMultiplier = 1f;

        private const float StutteringTimeMultiplier = 1.5f;

        private const float JitterTimeMultiplier = 0.75f;
        private const float JitterAmplitude = 80f;
        private const float JitterFrequency = 8f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InsulatedComponent, ElectrocutionAttemptEvent>(OnInsulatedElectrocutionAttempt);
            SubscribeLocalEvent<InsulatedComponent, ComponentGetState>(OnInsulatedGetState);
            SubscribeLocalEvent<InsulatedComponent, ComponentHandleState>(OnInsulatedHandleState);
        }

        public bool TryDoElectrocution(EntityUid uid, EntityUid? sourceUid, int shockDamage, TimeSpan time, float siemensCoefficient = 1f,
            StatusEffectsComponent? statusEffects = null,
            SharedPullableComponent? pullable = null,
            SharedPullerComponent? puller = null,
            SharedAlertsComponent? alerts = null)
        {
            var attemptEvent = new ElectrocutionAttemptEvent(uid, sourceUid, siemensCoefficient);
            RaiseLocalEvent(uid, attemptEvent);

            // Cancel the electrocution early, so we don't recursively electrocute anything.
            if (attemptEvent.Cancelled)
                return false;

            siemensCoefficient = attemptEvent.SiemensCoefficient;
            shockDamage = (int)(shockDamage * siemensCoefficient);

            if (shockDamage <= 0)
                return false;

            // Optional component.
            Resolve(uid, ref pullable, ref puller, ref alerts, false);

            var recursiveShockDamage = (int) (shockDamage * RecursiveDamageMultiplier);
            var recursiveTime = time * RecursiveTimeMultiplier;

            // If we're being pulled... Electrocute our puller too!
            if (pullable is { Puller: { Uid: var pullerUid } } && pullerUid != sourceUid)
            {
                TryDoElectrocution(pullerUid, sourceUid, recursiveShockDamage, recursiveTime, 1f);
            }

            // If we're pulling something... Electrocute that thing too!
            if (puller is { Pulling: { Uid: var pullingUid } } && pullingUid != sourceUid)
            {
                TryDoElectrocution(pullingUid, sourceUid, recursiveShockDamage, recursiveTime, 1f);
            }

            if (!Resolve(uid, ref statusEffects, false) || !_statusEffectsSystem.CanApplyEffect(uid, StatusEffectKey, statusEffects))
                return false;

            if (!_statusEffectsSystem.TryAddStatusEffect<ElectrocutedComponent>(uid, StatusEffectKey, time, statusEffects, alerts))
                return false;

            var shouldStun = siemensCoefficient > 0.5f;

            if (shouldStun)
                _stunSystem.TryParalyze(uid, time * ParalyzeTimeMultiplier, statusEffects, alerts);

            // TODO: Sparks here.

            _damageableSystem.TryChangeDamage(uid, new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(DamageType), shockDamage));
            _stutteringSystem.DoStutter(uid, time * StutteringTimeMultiplier, statusEffects, alerts);
            _jitteringSystem.DoJitter(uid, time * JitterTimeMultiplier, JitterAmplitude, JitterFrequency, true, statusEffects, alerts);

            _popupSystem.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-popup-player"), uid, Filter.Entities(uid).Unpredicted());

            var filter = Filter.Pvs(uid, 2f, EntityManager).RemoveWhereAttachedEntity(puid => puid == uid).Unpredicted();

            // TODO: Allow being able to pass EntityUid to Loc...
            if(sourceUid != null)
            {
                _popupSystem.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-by-source-popup-others",
                    ("mob", EntityManager.GetEntity(uid)), ("source", EntityManager.GetEntity(sourceUid.Value))), uid, filter);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("electrocuted-component-mob-shocked-popup-others",
                    ("mob", EntityManager.GetEntity(uid))), uid, filter);
            }

            RaiseLocalEvent(uid, new ElectrocutedEvent(uid, sourceUid, siemensCoefficient));

            return true;
        }

        public void SetInsulatedSiemensCoefficient(EntityUid uid, float siemensCoefficient, InsulatedComponent? insulated = null)
        {
            if (!Resolve(uid, ref insulated))
                return;

            insulated.SiemensCoefficient = siemensCoefficient;
            insulated.Dirty();
        }

        private void OnInsulatedElectrocutionAttempt(EntityUid uid, InsulatedComponent insulated, ElectrocutionAttemptEvent args)
        {
            args.SiemensCoefficient *= insulated.SiemensCoefficient;
        }

        private void OnInsulatedGetState(EntityUid uid, InsulatedComponent insulated, ref ComponentGetState args)
        {
            args.State = new InsulatedComponentState(insulated.SiemensCoefficient);
        }

        private void OnInsulatedHandleState(EntityUid uid, InsulatedComponent insulated, ref ComponentHandleState args)
        {
            if (args.Current is not InsulatedComponentState state)
                return;

            insulated.SiemensCoefficient = state.SiemensCoefficient;
        }

    }

    public class ElectrocutionAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid TargetUid;
        public readonly EntityUid? SourceUid;
        public float SiemensCoefficient = 1f;

        public ElectrocutionAttemptEvent(EntityUid targetUid, EntityUid? sourceUid, float siemensCoefficient)
        {
            TargetUid = targetUid;
            SourceUid = sourceUid;
            SiemensCoefficient = siemensCoefficient;
        }
    }

    public class ElectrocutedEvent : EntityEventArgs
    {
        public readonly EntityUid TargetUid;
        public readonly EntityUid? SourceUid;
        public readonly float SiemensCoefficient;

        public ElectrocutedEvent(EntityUid targetUid, EntityUid? sourceUid, float siemensCoefficient)
        {
            TargetUid = targetUid;
            SourceUid = sourceUid;
            SiemensCoefficient = siemensCoefficient;
        }
    }
}
