using System;
using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.MachineLinking.Events;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Light.EntitySystems
{
    /// <summary>
    ///     System for the PoweredLightComponens
    /// </summary>
    public class PoweredLightSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSystem = default!;
        [Dependency] private readonly LightBulbSystem _bulbSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly AdminLogSystem _logSystem = default!;

        private static readonly TimeSpan ThunkDelay = TimeSpan.FromSeconds(2);

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
            SubscribeLocalEvent<PoweredLightComponent, PacketSentEvent>(OnPacketReceived);

            SubscribeLocalEvent<PoweredLightComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnInit(EntityUid uid, PoweredLightComponent light, ComponentInit args)
        {
            light.LightBulbContainer = light.Owner.EnsureContainer<ContainerSlot>("light_bulb");
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
                    _popupSystem.PopupEntity(burnMsg, uid, Filter.Entities(userUid));

                    var damage = _damageableSystem.TryChangeDamage(userUid, light.Damage);

                    if (damage != null)
                        _logSystem.Add(LogType.Damaged,
                            $"{ToPrettyString(args.User):user} burned their hand on {ToPrettyString(args.Target):target} and received {damage.Total:damage} damage");

                    SoundSystem.Play(Filter.Pvs(uid), light.BurnHandSound.GetSound(), uid);

                    args.Handled = true;
                    return;
                }
            }

            // all checks passed
            // just try to eject bulb
            args.Handled = EjectBulb(uid, userUid, light) != null;
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
            if (GetBulb(uid, light) is not {Valid: true} bulb)
                return null;

            // try to remove bulb from container
            if (!light.LightBulbContainer.Remove(bulb))
                return null;

            // try to place bulb in hands
            if (userUid != null)
            {
                if (EntityManager.TryGetComponent(userUid.Value, out SharedHandsComponent? hands))
                    hands.PutInHand(bulb);
            }

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
                appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Empty);
                return;
            }
            else
            {
                powerReceiver.Load = (light.On && lightBulb.State == LightBulbState.Normal) ? lightBulb.PowerUse : 0;

                switch (lightBulb.State)
                {
                    case LightBulbState.Normal:
                        if (powerReceiver.Powered && light.On)
                        {
                            SetLight(uid, true, lightBulb.Color, light, lightBulb.LightRadius, lightBulb.LightEnergy, lightBulb.LightSoftness);
                            appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.On);
                            var time = _gameTiming.CurTime;
                            if (time > light.LastThunk + ThunkDelay)
                            {
                                light.LastThunk = time;
                                SoundSystem.Play(Filter.Pvs(uid), light.TurnOnSound.GetSound(), uid, AudioParams.Default.WithVolume(-10f));
                            }
                        }
                        else
                        {
                            SetLight(uid, false, light: light);
                            appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Off);
                        }
                        break;
                    case LightBulbState.Broken:
                        SetLight(uid, false, light: light);
                        appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Broken);
                        break;
                    case LightBulbState.Burned:
                        SetLight(uid, false, light: light);
                        appearance?.SetData(PoweredLightVisuals.BulbState, PoweredLightState.Burned);
                        break;
                }
            }
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

            ToggleBlinkingLight(light, true);
            light.Owner.SpawnTimer(light.GhostBlinkingTime, () =>
            {
                ToggleBlinkingLight(light, false);
            });

            args.Handled = true;
        }

        private void OnPowerChanged(EntityUid uid, PoweredLightComponent component, PowerChangedEvent args)
        {
            UpdateLight(uid, component);
        }

        public void ToggleBlinkingLight(PoweredLightComponent light, bool isNowBlinking)
        {
            if (light.IsBlinking == isNowBlinking)
                return;

            light.IsBlinking = isNowBlinking;

            if (!EntityManager.TryGetComponent(light.Owner, out AppearanceComponent? appearance))
                return;
            appearance.SetData(PoweredLightVisuals.Blinking, isNowBlinking);
        }

        private void OnSignalReceived(EntityUid uid, PoweredLightComponent component, SignalReceivedEvent args)
        {
            switch (args.Port)
            {
                case "state":
                    ToggleLight(uid, component);
                    break;
            }
        }

        /// <summary>
        /// Turns the light on or of when receiving a <see cref="DeviceNetworkConstants.CmdSetState"/> command.
        /// The light is turned on or of according to the <see cref="DeviceNetworkConstants.StateEnabled"/> value
         /// </summary>
        private void OnPacketReceived(EntityUid uid, PoweredLightComponent component, PacketSentEvent args)
        {
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) || command != DeviceNetworkConstants.CmdSetState) return;
            if (!args.Data.TryGetValue(DeviceNetworkConstants.StateEnabled, out bool enabled)) return;

            SetState(uid, enabled, component);
        }

        private void SetLight(EntityUid uid, bool value, Color? color = null, PoweredLightComponent? light = null, float? radius = null, float? energy = null, float? softness=null)
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
    }
}
