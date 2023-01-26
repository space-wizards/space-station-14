using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.DoAfter;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Threading;

namespace Content.Server.Light.EntitySystems
{
    /// <summary>
    ///     System for the PoweredLightComponents
    /// </summary>
    public sealed class PoweredLightSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSystem = default!;
        [Dependency] private readonly LightBulbSystem _bulbSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger= default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        private static readonly TimeSpan ThunkDelay = TimeSpan.FromSeconds(2);
        public const string LightBulbContainer = "light_bulb";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PoweredLightComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PoweredLightComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<PoweredLightComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PoweredLightComponent, InteractHandEvent>(OnInteractHand);

            SubscribeLocalEvent<PoweredLightComponent, GhostBooEvent>(OnGhostBoo);
            SubscribeLocalEvent<PoweredLightComponent, DamageChangedEvent>(HandleLightDamaged);

            SubscribeLocalEvent<PoweredLightComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<PoweredLightComponent, DeviceNetworkPacketEvent>(OnPacketReceived);

            SubscribeLocalEvent<PoweredLightComponent, PowerChangedEvent>(OnPowerChanged);

            SubscribeLocalEvent<PoweredLightComponent, EjectBulbCompleteEvent>(OnEjectBulbComplete);
            SubscribeLocalEvent<PoweredLightComponent, EjectBulbCancelledEvent>(OnEjectBulbCancelled);
        }

        private void OnInit(EntityUid uid, PoweredLightComponent light, ComponentInit args)
        {
            light.LightBulbContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, LightBulbContainer);
            _signalSystem.EnsureReceiverPorts(uid, light.OnPort, light.OffPort, light.TogglePort);
        }

        private void OnMapInit(EntityUid uid, PoweredLightComponent light, MapInitEvent args)
        {
            if (light.HasLampOnSpawn != null)
            {
                var entity = EntityManager.SpawnEntity(light.HasLampOnSpawn, EntityManager.GetComponent<TransformComponent>(light.Owner).Coordinates);
                light.LightBulbContainer.Insert(entity);
            }
            // need this to update visualizers
            UpdateLight(uid, light);
        }

        private void OnInteractUsing(EntityUid uid, PoweredLightComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = InsertBulb(uid, args.Used, component);
        }

        private void OnInteractHand(EntityUid uid, PoweredLightComponent light, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            if (light.CancelToken != null)
                return;

            // check if light has bulb to eject
            var bulbUid = GetBulb(uid, light);
            if (bulbUid == null)
                return;

            // check if it's possible to apply burn damage to user
            var userUid = args.User;
            if (EntityManager.TryGetComponent(userUid, out HeatResistanceComponent? heatResist) &&
                EntityManager.TryGetComponent(bulbUid.Value, out LightBulbComponent? lightBulb))
            {
                // get users heat resistance
                var res = heatResist.GetHeatResistance();

                // check heat resistance against user
                var burnedHand = light.CurrentLit && res < lightBulb.BurningTemperature;
                if (burnedHand)
                {
                    // apply damage to users hands and show message with sound
                    var burnMsg = Loc.GetString("powered-light-component-burn-hand");
                    _popupSystem.PopupEntity(burnMsg, uid, userUid);

                    var damage = _damageableSystem.TryChangeDamage(userUid, light.Damage, origin: userUid);

                    if (damage != null)
                        _adminLogger.Add(LogType.Damaged,
                            $"{ToPrettyString(args.User):user} burned their hand on {ToPrettyString(args.Target):target} and received {damage.Total:damage} damage");

                    SoundSystem.Play(light.BurnHandSound.GetSound(), Filter.Pvs(uid), uid);

                    args.Handled = true;
                    return;
                }
            }


            //removing a broken/burned bulb, so allow instant removal
            if(TryComp<LightBulbComponent>(bulbUid.Value, out var bulb) && bulb.State != LightBulbState.Normal)
            {
                args.Handled = EjectBulb(uid, userUid, light) != null;
                return;
            }

            // removing a working bulb, so require a delay
            light.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(userUid, light.EjectBulbDelay, light.CancelToken.Token, uid)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                TargetFinishedEvent = new EjectBulbCompleteEvent()
                {
                    Component = light,
                    User = userUid,
                    Target = uid,
                },
                TargetCancelledEvent = new EjectBulbCancelledEvent()
                {
                    Component = light,
                }
            });

            args.Handled = true;
        }

        #region Bulb Logic API
        /// <summary>
        ///     Inserts the bulb if possible.
        /// </summary>
        /// <returns>True if it could insert it, false if it couldn't.</returns>
        public bool InsertBulb(EntityUid uid, EntityUid bulbUid, PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return false;

            // check if light already has bulb
            if (GetBulb(uid, light) != null)
                return false;

            // check if bulb fits
            if (!EntityManager.TryGetComponent(bulbUid, out LightBulbComponent? lightBulb))
                return false;
            if (lightBulb.Type != light.BulbType)
                return false;

            // try to insert bulb in container
            if (!light.LightBulbContainer.Insert(bulbUid))
                return false;

            UpdateLight(uid, light);
            return true;
        }

        /// <summary>
        ///     Ejects the bulb to a mob's hand if possible.
        /// </summary>
        /// <returns>Bulb uid if it was successfully ejected, null otherwise</returns>
        public EntityUid? EjectBulb(EntityUid uid, EntityUid? userUid = null, PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return null;

            // check if light has bulb
            if (GetBulb(uid, light) is not { Valid: true } bulb)
                return null;

            // try to remove bulb from container
            if (!light.LightBulbContainer.Remove(bulb))
                return null;

            // try to place bulb in hands
            _handsSystem.PickupOrDrop(userUid, bulb);

            UpdateLight(uid, light);
            return bulb;
        }

        /// <summary>
        ///     Try to replace current bulb with a new one
        ///     If succeed old bulb just drops on floor
        /// </summary>
        public bool ReplaceBulb(EntityUid uid, EntityUid bulb, PoweredLightComponent? light = null)
        {
            EjectBulb(uid, null, light);
            return InsertBulb(uid, bulb, light);
        }

        /// <summary>
        ///     Try to get light bulb inserted in powered light
        /// </summary>
        /// <returns>Bulb uid if it exist, null otherwise</returns>
        public EntityUid? GetBulb(EntityUid uid, PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return null;

            return light.LightBulbContainer.ContainedEntity;
        }

        /// <summary>
        ///     Try to break bulb inside light fixture
        /// </summary>
        public void TryDestroyBulb(EntityUid uid, PoweredLightComponent? light = null)
        {
            // check bulb state
            var bulbUid = GetBulb(uid, light);
            if (bulbUid == null || !EntityManager.TryGetComponent(bulbUid.Value, out LightBulbComponent? lightBulb))
                return;
            if (lightBulb.State == LightBulbState.Broken)
                return;

            // break it
            _bulbSystem.SetState(bulbUid.Value, LightBulbState.Broken, lightBulb);
            _bulbSystem.PlayBreakSound(bulbUid.Value, lightBulb);
            UpdateLight(uid, light);
        }
        #endregion

        private void UpdateLight(EntityUid uid,
            PoweredLightComponent? light = null,
            ApcPowerReceiverComponent? powerReceiver = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref light, ref powerReceiver))
                return;

            // Optional component.
            Resolve(uid, ref appearance, false);

            // check if light has bulb
            var bulbUid = GetBulb(uid, light);
            if (bulbUid == null || !EntityManager.TryGetComponent(bulbUid.Value, out LightBulbComponent? lightBulb))
            {
                SetLight(uid, false, light: light);
                powerReceiver.Load = 0;
                _appearance.SetData(uid, PoweredLightVisuals.BulbState, PoweredLightState.Empty, appearance);
                return;
            }

            switch (lightBulb.State)
            {
                case LightBulbState.Normal:
                    if (powerReceiver.Powered && light.On)
                    {
                        SetLight(uid, true, lightBulb.Color, light, lightBulb.LightRadius, lightBulb.LightEnergy, lightBulb.LightSoftness);
                        _appearance.SetData(uid, PoweredLightVisuals.BulbState, PoweredLightState.On, appearance);
                        var time = _gameTiming.CurTime;
                        if (time > light.LastThunk + ThunkDelay)
                        {
                            light.LastThunk = time;
                            SoundSystem.Play(light.TurnOnSound.GetSound(), Filter.Pvs(uid), uid, AudioParams.Default.WithVolume(-10f));
                        }
                    }
                    else
                    {
                        SetLight(uid, false, light: light);
                        _appearance.SetData(uid, PoweredLightVisuals.BulbState, PoweredLightState.Off, appearance);
                    }
                    break;
                case LightBulbState.Broken:
                    SetLight(uid, false, light: light);
                    _appearance.SetData(uid, PoweredLightVisuals.BulbState, PoweredLightState.Broken, appearance);
                    break;
                case LightBulbState.Burned:
                    SetLight(uid, false, light: light);
                    _appearance.SetData(uid, PoweredLightVisuals.BulbState, PoweredLightState.Burned, appearance);
                    break;
            }

            powerReceiver.Load = (light.On && lightBulb.State == LightBulbState.Normal) ? lightBulb.PowerUse : 0;
        }

        /// <summary>
        ///     Destroy the light bulb if the light took any damage.
        /// </summary>
        public void HandleLightDamaged(EntityUid uid, PoweredLightComponent component, DamageChangedEvent args)
        {
            // Was it being repaired, or did it take damage?
            if (args.DamageIncreased)
            {
                // Eventually, this logic should all be done by this (or some other) system, not a component.
                TryDestroyBulb(uid, component);
            }
        }

        private void OnGhostBoo(EntityUid uid, PoweredLightComponent light, GhostBooEvent args)
        {
            if (light.IgnoreGhostsBoo)
                return;

            // check cooldown first to prevent abuse
            var time = _gameTiming.CurTime;
            if (light.LastGhostBlink != null)
            {
                if (time <= light.LastGhostBlink + light.GhostBlinkingCooldown)
                    return;
            }

            light.LastGhostBlink = time;

            ToggleBlinkingLight(uid, light, true);
            light.Owner.SpawnTimer(light.GhostBlinkingTime, () =>
            {
                ToggleBlinkingLight(uid, light, false);
            });

            args.Handled = true;
        }

        private void OnPowerChanged(EntityUid uid, PoweredLightComponent component, ref PowerChangedEvent args)
        {
            UpdateLight(uid, component);
        }

        public void ToggleBlinkingLight(EntityUid uid, PoweredLightComponent light, bool isNowBlinking)
        {
            if (light.IsBlinking == isNowBlinking)
                return;

            light.IsBlinking = isNowBlinking;

            if (!EntityManager.TryGetComponent(light.Owner, out AppearanceComponent? appearance))
                return;

            _appearance.SetData(uid, PoweredLightVisuals.Blinking, isNowBlinking, appearance);
        }

        private void OnSignalReceived(EntityUid uid, PoweredLightComponent component, SignalReceivedEvent args)
        {
            if (args.Port == component.OffPort)
                SetState(uid, false, component);
            else if (args.Port == component.OnPort)
                SetState(uid, true, component);
            else if (args.Port == component.TogglePort)
                ToggleLight(uid, component);
        }

        /// <summary>
        /// Turns the light on or of when receiving a <see cref="DeviceNetworkConstants.CmdSetState"/> command.
        /// The light is turned on or of according to the <see cref="DeviceNetworkConstants.StateEnabled"/> value
        /// </summary>
        private void OnPacketReceived(EntityUid uid, PoweredLightComponent component, DeviceNetworkPacketEvent args)
        {
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) || command != DeviceNetworkConstants.CmdSetState) return;
            if (!args.Data.TryGetValue(DeviceNetworkConstants.StateEnabled, out bool enabled)) return;

            SetState(uid, enabled, component);
        }

        private void SetLight(EntityUid uid, bool value, Color? color = null, PoweredLightComponent? light = null, float? radius = null, float? energy = null, float? softness = null)
        {
            if (!Resolve(uid, ref light))
                return;

            light.CurrentLit = value;
            _ambientSystem.SetAmbience(uid, value);

            if (EntityManager.TryGetComponent(uid, out PointLightComponent? pointLight))
            {
                pointLight.Enabled = value;

                if (color != null)
                    pointLight.Color = color.Value;
                if (radius != null)
                    pointLight.Radius = (float) radius;
                if (energy != null)
                    pointLight.Energy = (float) energy;
                if (softness != null)
                    pointLight.Softness = (float) softness;
            }
        }

        public void ToggleLight(EntityUid uid, PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return;

            light.On = !light.On;
            UpdateLight(uid, light);
        }

        public void SetState(EntityUid uid, bool state, PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return;

            light.On = state;
            UpdateLight(uid, light);
        }

        private void OnEjectBulbComplete(EntityUid uid, PoweredLightComponent component, EjectBulbCompleteEvent args)
        {
            args.Component.CancelToken = null;
            EjectBulb(args.Target, args.User, args.Component);
        }

        private static void OnEjectBulbCancelled(EntityUid uid, PoweredLightComponent component, EjectBulbCancelledEvent args)
        {
            args.Component.CancelToken = null;
        }

        private sealed class EjectBulbCompleteEvent : EntityEventArgs
        {
            public PoweredLightComponent Component { get; init; } = default!;
            public EntityUid User { get; init; }
            public EntityUid Target { get; init; }
        }

        private sealed class EjectBulbCancelledEvent : EntityEventArgs
        {
            public PoweredLightComponent Component { get; init; } = default!;
        }
    }
}
