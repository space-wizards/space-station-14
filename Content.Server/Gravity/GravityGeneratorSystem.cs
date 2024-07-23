using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Shared.Database;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Gravity
{
    public sealed class GravityGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly GravitySystem _gravitySystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedPointLightSystem _lights = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GravityGeneratorComponent, ComponentInit>(OnCompInit);
            SubscribeLocalEvent<GravityGeneratorComponent, ComponentShutdown>(OnComponentShutdown);
            SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged); // Or just anchor changed?
            SubscribeLocalEvent<GravityGeneratorComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<GravityGeneratorComponent, SharedGravityGeneratorComponent.SwitchGeneratorMessage>(
                OnSwitchGenerator);
        }

        private void OnParentChanged(EntityUid uid, GravityGeneratorComponent component, ref EntParentChangedMessage args)
        {
            if (component.GravityActive && TryComp(args.OldParent, out GravityComponent? gravity))
            {
                _gravitySystem.RefreshGravity(args.OldParent.Value, gravity);
            }
        }

        private void OnComponentShutdown(EntityUid uid, GravityGeneratorComponent component, ComponentShutdown args)
        {
            if (component.GravityActive &&
                TryComp(uid, out TransformComponent? xform) &&
                TryComp(xform.ParentUid, out GravityComponent? gravity))
            {
                component.GravityActive = false;
                _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<GravityGeneratorComponent, ApcPowerReceiverComponent>();
            while (query.MoveNext(out var uid, out var gravGen, out var powerReceiver))
            {
                var ent = (uid, gravGen, powerReceiver);
                if (!gravGen.Intact)
                    continue;

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

                var active = gravGen.GravityActive;
                var lastCharge = gravGen.Charge;
                gravGen.Charge = Math.Clamp(gravGen.Charge + frameTime * chargeRate, 0, gravGen.MaxCharge);
                if (chargeRate > 0)
                {
                    // Charging.
                    if (MathHelper.CloseTo(gravGen.Charge, gravGen.MaxCharge) && !gravGen.GravityActive)
                    {
                        gravGen.GravityActive = true;
                    }
                }
                else
                {
                    // Discharging
                    if (MathHelper.CloseTo(gravGen.Charge, 0) && gravGen.GravityActive)
                    {
                        gravGen.GravityActive = false;
                    }
                }

                var updateUI = gravGen.NeedUIUpdate;
                if (!MathHelper.CloseTo(lastCharge, gravGen.Charge))
                {
                    UpdateState(ent);
                    updateUI = true;
                }

                if (updateUI)
                    UpdateUI(ent, chargeRate);

                if (active != gravGen.GravityActive &&
                    TryComp(uid, out TransformComponent? xform) &&
                    TryComp<GravityComponent>(xform.ParentUid, out var gravity))
                {
                    // Force it on in the faster path.
                    if (gravGen.GravityActive)
                    {
                        _gravitySystem.EnableGravity(xform.ParentUid, gravity);
                    }
                    else
                    {
                        _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
                    }
                }
            }
        }

        private void SetSwitchedOn(EntityUid uid, GravityGeneratorComponent component, bool on,
            ApcPowerReceiverComponent? powerReceiver = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref powerReceiver))
                return;

            if (user != null)
                _adminLogger.Add(LogType.Action, on ? LogImpact.Medium : LogImpact.High, $"{ToPrettyString(user)} set ${ToPrettyString(uid):target} to {(on ? "on" : "off")}");

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

        private void UpdateUI(Entity<GravityGeneratorComponent, ApcPowerReceiverComponent> ent, float chargeRate)
        {
            var (_, component, powerReceiver) = ent;
            if (!_uiSystem.IsUiOpen(ent.Owner, SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key))
                return;

            var chargeTarget = chargeRate < 0 ? 0 : component.MaxCharge;
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

            _uiSystem.SetUiState(
                ent.Owner,
                SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key,
                state);

            component.NeedUIUpdate = false;
        }

        private void OnCompInit(Entity<GravityGeneratorComponent> ent, ref ComponentInit args)
        {
            ApcPowerReceiverComponent? powerReceiver = null;
            if (!Resolve(ent, ref powerReceiver, false))
                return;

            UpdatePowerState(ent, powerReceiver);
            UpdateState((ent, ent.Comp, powerReceiver));
        }

        private void OnInteractHand(EntityUid uid, GravityGeneratorComponent component, InteractHandEvent args)
        {
            ApcPowerReceiverComponent? powerReceiver = default!;
            if (!Resolve(uid, ref powerReceiver))
                return;

            // Do not allow opening UI if broken or unpowered.
            if (!component.Intact || powerReceiver.PowerReceived < component.IdlePowerUse)
                return;

            _uiSystem.OpenUi(uid, SharedGravityGeneratorComponent.GravityGeneratorUiKey.Key, args.User);
            component.NeedUIUpdate = true;
        }

        public void UpdateState(Entity<GravityGeneratorComponent, ApcPowerReceiverComponent> ent)
        {
            var (uid, grav, powerReceiver) = ent;
            var appearance = EntityManager.GetComponentOrNull<AppearanceComponent>(uid);
            _appearance.SetData(uid, GravityGeneratorVisuals.Charge, grav.Charge, appearance);

            if (_lights.TryGetLight(uid, out var pointLight))
            {
                _lights.SetEnabled(uid, grav.Charge > 0, pointLight);
                _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, grav.Charge), pointLight);
            }

            if (!grav.Intact)
            {
                MakeBroken((uid, grav), appearance);
            }
            else if (powerReceiver.PowerReceived < grav.IdlePowerUse)
            {
                MakeUnpowered((uid, grav), appearance);
            }
            else if (!grav.SwitchedOn)
            {
                MakeOff((uid, grav), appearance);
            }
            else
            {
                MakeOn((uid, grav), appearance);
            }
        }

        private void MakeBroken(Entity<GravityGeneratorComponent> ent, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(ent, false);

            _appearance.SetData(ent, GravityGeneratorVisuals.State, GravityGeneratorStatus.Broken);
        }

        private void MakeUnpowered(Entity<GravityGeneratorComponent> ent, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(ent, false);

            _appearance.SetData(ent, GravityGeneratorVisuals.State, GravityGeneratorStatus.Unpowered, appearance);
        }

        private void MakeOff(Entity<GravityGeneratorComponent> ent, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(ent, false);

            _appearance.SetData(ent, GravityGeneratorVisuals.State, GravityGeneratorStatus.Off, appearance);
        }

        private void MakeOn(Entity<GravityGeneratorComponent> ent, AppearanceComponent? appearance)
        {
            _ambientSoundSystem.SetAmbience(ent, true);

            _appearance.SetData(ent, GravityGeneratorVisuals.State, GravityGeneratorStatus.On, appearance);
        }

        private void OnSwitchGenerator(
            EntityUid uid,
            GravityGeneratorComponent component,
            SharedGravityGeneratorComponent.SwitchGeneratorMessage args)
        {
            SetSwitchedOn(uid, component, args.On, user: args.Actor);
        }
    }
}
