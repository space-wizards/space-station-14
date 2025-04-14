using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Emp;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Power;

namespace Content.Server.Light.EntitySystems
{
    /// <summary>
    ///     System for the PoweredLightComponents
    /// </summary>
    public sealed class PoweredLightSystem : SharedPoweredLightSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSystem = default!;
        [Dependency] private readonly LightBulbSystem _bulbSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly PointLightSystem _pointLight = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly DamageOnInteractSystem _damageOnInteractSystem = default!;

        private static readonly TimeSpan ThunkDelay = TimeSpan.FromSeconds(2);
        public const string LightBulbContainer = "light_bulb";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, InteractHandEvent>(OnInteractHand);

            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, GhostBooEvent>(OnGhostBoo);
            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, DamageChangedEvent>(HandleLightDamaged);

            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, DeviceNetworkPacketEvent>(OnPacketReceived);

            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, PowerChangedEvent>(OnPowerChanged);

            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, PoweredLightDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<Shared.Light.Components.PoweredLightComponent, EmpPulseEvent>(OnEmpPulse);
        }

        private void OnInit(EntityUid uid, Shared.Light.Components.PoweredLightComponent light, ComponentInit args)
        {
            light.LightBulbContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, LightBulbContainer);
            _signalSystem.EnsureSinkPorts(uid, light.OnPort, light.OffPort, light.TogglePort);
        }

        private void OnMapInit(EntityUid uid, Shared.Light.Components.PoweredLightComponent light, MapInitEvent args)
        {
            // TODO: Use ContainerFill dog
            if (light.HasLampOnSpawn != null)
            {
                var entity = EntityManager.SpawnEntity(light.HasLampOnSpawn, EntityManager.GetComponent<TransformComponent>(uid).Coordinates);
                _containerSystem.Insert(entity, light.LightBulbContainer);
            }
            // need this to update visualizers
            UpdateLight(uid, light);
        }

        #region Bulb Logic API
        /// <summary>
        ///     Inserts the bulb if possible.
        /// </summary>
        /// <returns>True if it could insert it, false if it couldn't.</returns>
        public bool InsertBulb(EntityUid uid, EntityUid bulbUid, Shared.Light.Components.PoweredLightComponent? light = null)
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
            if (!_containerSystem.Insert(bulbUid, light.LightBulbContainer))
                return false;

            UpdateLight(uid, light);
            return true;
        }

        /// <summary>
        ///     Ejects the bulb to a mob's hand if possible.
        /// </summary>
        /// <returns>Bulb uid if it was successfully ejected, null otherwise</returns>
        public EntityUid? EjectBulb(EntityUid uid, EntityUid? userUid = null, Shared.Light.Components.PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return null;

            // check if light has bulb
            if (GetBulb(uid, light) is not { Valid: true } bulb)
                return null;

            // try to remove bulb from container
            if (!_containerSystem.Remove(bulb, light.LightBulbContainer))
                return null;

            // try to place bulb in hands
            _handsSystem.PickupOrDrop(userUid, bulb);

            UpdateLight(uid, light);
            return bulb;
        }

        /// <summary>
        ///     Replaces the spawned prototype of a pre-mapinit powered light with a different variant.
        /// </summary>
        public bool ReplaceSpawnedPrototype(Entity<Shared.Light.Components.PoweredLightComponent> light, string bulb)
        {
            if (light.Comp.LightBulbContainer.ContainedEntity != null)
                return false;

            if (LifeStage(light.Owner) >= EntityLifeStage.MapInitialized)
                return false;

            light.Comp.HasLampOnSpawn = bulb;
            return true;
        }

        /// <summary>
        ///     Try to replace current bulb with a new one
        ///     If succeed old bulb just drops on floor
        /// </summary>
        public bool ReplaceBulb(EntityUid uid, EntityUid bulb, Shared.Light.Components.PoweredLightComponent? light = null)
        {
            EjectBulb(uid, null, light);
            return InsertBulb(uid, bulb, light);
        }

        /// <summary>
        ///     Try to get light bulb inserted in powered light
        /// </summary>
        /// <returns>Bulb uid if it exist, null otherwise</returns>
        public EntityUid? GetBulb(EntityUid uid, Shared.Light.Components.PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return null;

            return light.LightBulbContainer.ContainedEntity;
        }

        /// <summary>
        ///     Try to break bulb inside light fixture
        /// </summary>
        public bool TryDestroyBulb(EntityUid uid, Shared.Light.Components.PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light, false))
                return false;

            // if we aren't mapinited,
            // just null the spawned bulb
            if (LifeStage(uid) < EntityLifeStage.MapInitialized)
            {
                light.HasLampOnSpawn = null;
                return true;
            }

            // check bulb state
            var bulbUid = GetBulb(uid, light);
            if (bulbUid == null || !EntityManager.TryGetComponent(bulbUid.Value, out LightBulbComponent? lightBulb))
                return false;
            if (lightBulb.State == LightBulbState.Broken)
                return false;

            // break it
            _bulbSystem.SetState(bulbUid.Value, LightBulbState.Broken, lightBulb);
            _bulbSystem.PlayBreakSound(bulbUid.Value, lightBulb);
            UpdateLight(uid, light);
            return true;
        }
        #endregion

        private void UpdateLight(EntityUid uid,
            Shared.Light.Components.PoweredLightComponent? light = null,
            ApcPowerReceiverComponent? powerReceiver = null,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref light, ref powerReceiver, false))
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
                            _audio.PlayPvs(light.TurnOnSound, uid, light.TurnOnSound.Params.AddVolume(-10f));
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
        public void HandleLightDamaged(EntityUid uid, Shared.Light.Components.PoweredLightComponent component, DamageChangedEvent args)
        {
            // Was it being repaired, or did it take damage?
            if (args.DamageIncreased)
            {
                // Eventually, this logic should all be done by this (or some other) system, not a component.
                TryDestroyBulb(uid, component);
            }
        }

        private void OnGhostBoo(EntityUid uid, Shared.Light.Components.PoweredLightComponent light, GhostBooEvent args)
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
            uid.SpawnTimer(light.GhostBlinkingTime, () =>
            {
                ToggleBlinkingLight(uid, light, false);
            });

            args.Handled = true;
        }

        private void OnPowerChanged(EntityUid uid, Shared.Light.Components.PoweredLightComponent component, ref PowerChangedEvent args)
        {
            // TODO: Power moment
            var metadata = MetaData(uid);

            if (metadata.EntityPaused || TerminatingOrDeleted(uid, metadata))
                return;

            UpdateLight(uid, component);
        }

        public void ToggleBlinkingLight(EntityUid uid, Shared.Light.Components.PoweredLightComponent light, bool isNowBlinking)
        {
            if (light.IsBlinking == isNowBlinking)
                return;

            light.IsBlinking = isNowBlinking;

            if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            _appearance.SetData(uid, PoweredLightVisuals.Blinking, isNowBlinking, appearance);
        }

        private void OnSignalReceived(EntityUid uid, Shared.Light.Components.PoweredLightComponent component, ref SignalReceivedEvent args)
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
        private void OnPacketReceived(EntityUid uid, Shared.Light.Components.PoweredLightComponent component, DeviceNetworkPacketEvent args)
        {
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) || command != DeviceNetworkConstants.CmdSetState) return;
            if (!args.Data.TryGetValue(DeviceNetworkConstants.StateEnabled, out bool enabled)) return;

            SetState(uid, enabled, component);
        }

        private void SetLight(EntityUid uid, bool value, Color? color = null, Shared.Light.Components.PoweredLightComponent? light = null, float? radius = null, float? energy = null, float? softness = null)
        {
            if (!Resolve(uid, ref light))
                return;

            light.CurrentLit = value;
            _ambientSystem.SetAmbience(uid, value);

            if (EntityManager.TryGetComponent(uid, out PointLightComponent? pointLight))
            {
                _pointLight.SetEnabled(uid, value, pointLight);

                if (color != null)
                    _pointLight.SetColor(uid, color.Value, pointLight);
                if (radius != null)
                    _pointLight.SetRadius(uid, (float) radius, pointLight);
                if (energy != null)
                    _pointLight.SetEnergy(uid, (float) energy, pointLight);
                if (softness != null)
                    _pointLight.SetSoftness(uid, (float) softness, pointLight);
            }

            // light bulbs burn your hands!
            if (TryComp<DamageOnInteractComponent>(uid, out var damageOnInteractComp))
                _damageOnInteractSystem.SetIsDamageActiveTo((uid, damageOnInteractComp), value);
        }

        public void ToggleLight(EntityUid uid, Shared.Light.Components.PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return;

            light.On = !light.On;
            UpdateLight(uid, light);
        }

        public void SetState(EntityUid uid, bool state, Shared.Light.Components.PoweredLightComponent? light = null)
        {
            if (!Resolve(uid, ref light))
                return;

            light.On = state;
            UpdateLight(uid, light);
        }

        private void OnDoAfter(EntityUid uid, Shared.Light.Components.PoweredLightComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null)
                return;

            EjectBulb(args.Args.Target.Value, args.Args.User, component);

            args.Handled = true;
        }

        private void OnEmpPulse(EntityUid uid, Shared.Light.Components.PoweredLightComponent component, ref EmpPulseEvent args)
        {
            if (TryDestroyBulb(uid, component))
                args.Affected = true;
        }
    }
}
