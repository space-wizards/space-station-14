using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Gravity.EntitySystems
{
    public sealed class GravityGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly GravitySystem _gravitySystem = default!;
        [Dependency] private readonly GravityShakeSystem _gravityShakeSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GravityGeneratorComponent, ComponentInit>(OnComponentInitialized);
            SubscribeLocalEvent<GravityGeneratorComponent, ComponentShutdown>(OnComponentShutdown);
            SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged); // Or just anchor changed?
            SubscribeLocalEvent<GravityGeneratorComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<GravityGeneratorComponent, SharedGravityGeneratorComponent.SwitchGeneratorMessage>(
                OnSwitchGenerator);
        }

        private void OnParentChanged(EntityUid uid, GravityGeneratorComponent component, ref EntParentChangedMessage args)
        {
            // TODO consider stations with more than one generator.
            if (component.GravityActive && TryComp(args.OldParent, out GravityComponent? gravity))
                _gravitySystem.DisableGravity(gravity);

            UpdateGravityActive(component, false);
        }

        private void OnComponentShutdown(EntityUid uid, GravityGeneratorComponent component, ComponentShutdown args)
        {
            component.GravityActive = false;
            UpdateGravityActive(component, true);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (gravGen, powerReceiver) in EntityManager
                .EntityQuery<GravityGeneratorComponent, ApcPowerReceiverComponent>())
            {
                if (!gravGen.Intact)
                    return;

                // Calculate charge rate based on power state and such.
                // Negative charge rate means discharging.
                float chargeRate;
                if (gravGen.SwitchedOn)
                {
                    if (powerReceiver.Powered)
                    {
                        chargeRate = gravGen.ChargeRate;
                    }
                    else
                    {
                        // Scale discharge rate such that if we're at 25% active power we discharge at 75% rate.
                        var receiving = powerReceiver.PowerReceived;
                        var mainSystemPower = Math.Max(0, receiving - gravGen.IdlePowerUse);
                        var ratio = 1 - mainSystemPower / (gravGen.ActivePowerUse - gravGen.IdlePowerUse);
                        chargeRate = -(ratio * gravGen.ChargeRate);
                    }
                }
                else
                {
                    chargeRate = -gravGen.ChargeRate;
                }

                var updateGravity = gravGen.NeedGravityUpdate;
                var shakeGravity = false;
                var lastCharge = gravGen.Charge;
                gravGen.Charge = Math.Clamp(gravGen.Charge + frameTime * chargeRate, 0, 1);
                if (chargeRate > 0)
                {
                    // Charging.
                    if (MathHelper.CloseTo(gravGen.Charge, 1) && !gravGen.GravityActive)
                    {
                        shakeGravity = true;
                        updateGravity = true;
                        gravGen.GravityActive = true;
                    }
                }
                else
                {
                    // Discharging
                    if (MathHelper.CloseTo(gravGen.Charge, 0) && gravGen.GravityActive)
                    {
                        shakeGravity = true;
                        updateGravity = true;
                        gravGen.GravityActive = false;
                    }
                }

                var updateUI = gravGen.NeedUIUpdate;
                if (!MathHelper.CloseTo(lastCharge, gravGen.Charge))
                {
                    UpdateState(gravGen, powerReceiver);
                    updateUI = true;
                }

                if (updateUI)
                    UpdateUI(gravGen, powerReceiver, chargeRate);

                if (updateGravity)
                {
                    UpdateGravityActive(gravGen, shakeGravity);
                }
            }
        }

        private void SetSwitchedOn(EntityUid uid, GravityGeneratorComponent component, bool on, ApcPowerReceiverComponent? powerReceiver = null)
        {
            if (!Resolve(uid, ref powerReceiver))
                return;

            component.SwitchedOn = on;
            UpdatePowerState(component, powerReceiver);
            component.NeedUIUpdate = true;
        }

        private static void UpdatePowerState(
            GravityGeneratorComponent component,
            ApcPowerReceiverComponent powerReceiver)
        {
            powerReceiver.Load = component.SwitchedOn ? component.ActivePowerUse : component.IdlePowerUse;
        }

        private void UpdateUI(
            GravityGeneratorComponent component,
            ApcPowerReceiverComponent powerReceiver,
            float chargeRate)
        {
            if (!_uiSystem.IsUiOpen(component.Owner, SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key))
                return;

            var chargeTarget = chargeRate < 0 ? 0 : 1;
            short chargeEta;
            var atTarget = false;
            if (MathHelper.CloseTo(component.Charge, chargeTarget))
            {
                chargeEta = short.MinValue; // N/A
                atTarget = true;
            }
            else
            {
                var diff = chargeTarget - component.Charge;
                chargeEta = (short) Math.Abs(diff / chargeRate);
            }

            var status = chargeRate switch
            {
                > 0 when atTarget => GravityGeneratorPowerStatus.FullyCharged,
                < 0 when atTarget => GravityGeneratorPowerStatus.Off,
                > 0 => GravityGeneratorPowerStatus.Charging,
                < 0 => GravityGeneratorPowerStatus.Discharging,
                _ => throw new ArgumentOutOfRangeException()
            };

            var state = new SharedGravityGeneratorComponent.GeneratorState(
                component.SwitchedOn,
                (byte) (component.Charge * 255),
                status,
                (short) Math.Round(powerReceiver.PowerReceived),
                (short) Math.Round(powerReceiver.Load),
                chargeEta
            );

            _uiSystem.TrySetUiState(
                component.Owner,
                SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key,
                state);

            component.NeedUIUpdate = false;
        }

        private void OnComponentInitialized(EntityUid uid, GravityGeneratorComponent component, ComponentInit args)
        {
            // Always update gravity on init.
            component.NeedGravityUpdate = true;

            ApcPowerReceiverComponent? powerReceiver = null;
            if (!Resolve(uid, ref powerReceiver, false))
                return;

            UpdatePowerState(component, powerReceiver);
            UpdateState(component, powerReceiver);
        }

        private void UpdateGravityActive(GravityGeneratorComponent grav, bool shake)
        {
            var gridId = EntityManager.GetComponent<TransformComponent>(grav.Owner).GridUid;
            if (!_mapManager.TryGetGrid(gridId, out var grid))
                return;

            var gravity = EntityManager.GetComponent<GravityComponent>(gridId.Value);

            if (grav.GravityActive)
                _gravitySystem.EnableGravity(gravity);
            else
                _gravitySystem.DisableGravity(gravity);

            if (shake)
                _gravityShakeSystem.ShakeGrid(gridId.Value, gravity);
        }

        private void OnInteractHand(EntityUid uid, GravityGeneratorComponent component, InteractHandEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            ApcPowerReceiverComponent? powerReceiver = default!;
            if (!Resolve(uid, ref powerReceiver))
                return;

            // Do not allow opening UI if broken or unpowered.
            if (!component.Intact || powerReceiver.PowerReceived < component.IdlePowerUse)
                return;

            _uiSystem.TryOpen(uid, SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key, actor.PlayerSession);
            component.NeedUIUpdate = true;
        }

        public void UpdateState(GravityGeneratorComponent grav, ApcPowerReceiverComponent powerReceiver)
        {
            var uid = grav.Owner;
            var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(uid);
            appearance?.SetData(GravityGeneratorVisuals.Charge, grav.Charge);

            if (EntityManager.TryGetComponent(uid, out PointLightComponent? pointLight))
            {
                pointLight.Enabled = grav.Charge > 0;
                pointLight.Radius = MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, grav.Charge);
            }

            if (!grav.Intact)
            {
                MakeBroken(grav, appearance);
            }
            else if (powerReceiver.PowerReceived < grav.IdlePowerUse)
            {
                MakeUnpowered(grav, appearance);
            }
            else if (!grav.SwitchedOn)
            {
                MakeOff(grav, appearance);
            }
            else
            {
                MakeOn(grav, appearance);
            }
        }

        private void MakeBroken(GravityGeneratorComponent component, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(component.Owner, false);

            appearance?.SetData(GravityGeneratorVisuals.State, GravityGeneratorStatus.Broken);
        }

        private void MakeUnpowered(GravityGeneratorComponent component, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(component.Owner, false);

            appearance?.SetData(GravityGeneratorVisuals.State, GravityGeneratorStatus.Unpowered);
        }

        private void MakeOff(GravityGeneratorComponent component, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(component.Owner, false);

            appearance?.SetData(GravityGeneratorVisuals.State, GravityGeneratorStatus.Off);
        }

        private void MakeOn(GravityGeneratorComponent component, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(component.Owner, true);

            appearance?.SetData(GravityGeneratorVisuals.State, GravityGeneratorStatus.On);
        }

        private void OnSwitchGenerator(
            EntityUid uid,
            GravityGeneratorComponent component,
            SharedGravityGeneratorComponent.SwitchGeneratorMessage args)
        {
            SetSwitchedOn(uid, component, args.On);
        }
    }
}
