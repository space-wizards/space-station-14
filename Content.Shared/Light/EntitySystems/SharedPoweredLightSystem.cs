using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Light.EntitySystems;

public abstract class SharedPoweredLightSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly DamageOnInteractSystem _damageOnInteractSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedLightBulbSystem _bulbSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;

    private static readonly TimeSpan ThunkDelay = TimeSpan.FromSeconds(2);
    public const string LightBulbContainer = "light_bulb";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoweredLightComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PoweredLightComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<PoweredLightComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<PoweredLightComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PoweredLightComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PoweredLightComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<PoweredLightComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<PoweredLightComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<PoweredLightComponent, PoweredLightDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PoweredLightComponent, DamageChangedEvent>(HandleLightDamaged);
    }

    private void OnInit(EntityUid uid, PoweredLightComponent light, ComponentInit args)
    {
        light.LightBulbContainer = ContainerSystem.EnsureContainer<ContainerSlot>(uid, LightBulbContainer);
        _deviceLink.EnsureSinkPorts(uid, light.OnPort, light.OffPort, light.TogglePort);
    }

    private void OnRemoved(Entity<PoweredLightComponent> light, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != LightBulbContainer)
            return;

        UpdateLight(light, light);
    }

    private void OnInserted(Entity<PoweredLightComponent> light, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != LightBulbContainer)
            return;

        UpdateLight(light, light);
    }

    private void OnInteractUsing(EntityUid uid, PoweredLightComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = InsertBulb(uid, args.Used, component, user: args.User, playAnimation: true);
    }

    private void OnInteractHand(EntityUid uid, PoweredLightComponent light, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        // check if light has bulb to eject
        var bulbUid = GetBulb(uid, light);
        if (bulbUid == null)
            return;

        var userUid = args.User;
        //removing a broken/burned bulb, so allow instant removal
        if (TryComp<LightBulbComponent>(bulbUid.Value, out var bulb) && bulb.State != LightBulbState.Normal)
        {
            args.Handled = EjectBulb(uid, userUid, light) != null;
            return;
        }

        // removing a working bulb, so require a delay
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, userUid, light.EjectBulbDelay, new PoweredLightDoAfterEvent(), uid, target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });

        args.Handled = true;
    }

    private void OnSignalReceived(Entity<PoweredLightComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ent.Comp.OffPort)
            SetState(ent, false, ent.Comp);
        else if (args.Port == ent.Comp.OnPort)
            SetState(ent, true, ent.Comp);
        else if (args.Port == ent.Comp.TogglePort)
            ToggleLight(ent, ent.Comp);
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

    /// <summary>
    ///     Inserts the bulb if possible.
    /// </summary>
    /// <returns>True if it could insert it, false if it couldn't.</returns>
    public bool InsertBulb(EntityUid uid, EntityUid bulbUid, PoweredLightComponent? light = null, EntityUid? user = null, bool playAnimation = false)
    {
        if (!Resolve(uid, ref light))
            return false;

        // check if light already has bulb
        if (GetBulb(uid, light) != null)
            return false;

        // check if bulb fits
        if (!TryComp<LightBulbComponent>(bulbUid, out var lightBulb))
            return false;

        if (lightBulb.Type != light.BulbType)
            return false;

        // try to insert bulb in container
        if (!ContainerSystem.Insert(bulbUid, light.LightBulbContainer))
            return false;

        if (playAnimation && TryComp(user, out TransformComponent? xform))
        {
            var itemXform = Transform(uid);
            _storage.PlayPickupAnimation(bulbUid, xform.Coordinates, itemXform.Coordinates, itemXform.LocalRotation, user: user);
        }

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
        if (!ContainerSystem.Remove(bulb, light.LightBulbContainer))
            return null;

        // try to place bulb in hands
        _handsSystem.PickupOrDrop(userUid, bulb);

        return bulb;
    }

    /// <summary>
    ///     Replaces the spawned prototype of a pre-mapinit powered light with a different variant.
    /// </summary>
    public bool ReplaceSpawnedPrototype(Entity<PoweredLightComponent> light, string bulb)
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

        return light.LightBulbContainer?.ContainedEntity;
    }

    /// <summary>
    ///     Try to break bulb inside light fixture
    /// </summary>
    public bool TryDestroyBulb(EntityUid uid, PoweredLightComponent? light = null)
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

    protected void UpdateLight(EntityUid uid,
        PoweredLightComponent? light = null,
        SharedApcPowerReceiverComponent? powerReceiver = null,
        AppearanceComponent? appearance = null,
        EntityUid? user = null)
    {
        // We don't do anything during state application on the client as if
        // it's due to an entity spawn, we'd have to wait for component init to
        // be able to do anything, despite the server having already sent us the
        // state that we need. On the other hand, we still want this to run in
        // prediction so we can, well, predict lights turning on.
        if (GameTiming.ApplyingState)
            return;

        if (!Resolve(uid, ref light, false))
            return;

        if (!_receiver.ResolveApc(uid, ref powerReceiver))
            return;

        // Optional component.
        Resolve(uid, ref appearance, false);

        // check if light has bulb
        var bulbUid = GetBulb(uid, light);
        if (bulbUid == null || !TryComp<LightBulbComponent>(bulbUid.Value, out var lightBulb))
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
                    var time = GameTiming.CurTime;
                    if (time > light.LastThunk + ThunkDelay)
                    {
                        light.LastThunk = time;
                        Dirty(uid, light);
                        _audio.PlayPredicted(light.TurnOnSound, uid, user: user, light.TurnOnSound.Params.AddVolume(-10f));
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

    private void OnPowerChanged(EntityUid uid, PoweredLightComponent component, ref PowerChangedEvent args)
    {
        // TODO: Power moment
        var metadata = MetaData(uid);

        if (metadata.EntityPaused || TerminatingOrDeleted(uid, metadata))
            return;

        UpdateLight(uid, component);
    }

    public void ToggleBlinkingLight(EntityUid uid, PoweredLightComponent light, bool isNowBlinking)
    {
        if (light.IsBlinking == isNowBlinking)
            return;

        light.IsBlinking = isNowBlinking;
        Dirty(uid, light);

        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, PoweredLightVisuals.Blinking, isNowBlinking, appearance);
    }

    private void SetLight(EntityUid uid, bool value, Color? color = null, PoweredLightComponent? light = null, float? radius = null, float? energy = null, float? softness = null)
    {
        if (!Resolve(uid, ref light))
            return;

        if (light.CurrentLit != value)
        {
            light.CurrentLit = value;
            Dirty(uid, light);
        }

        _ambientSystem.SetAmbience(uid, value);

        if (_pointLight.TryGetLight(uid, out var pointLight))
        {
            _pointLight.SetEnabled(uid, value, pointLight);

            if (color != null)
                _pointLight.SetColor(uid, color.Value, pointLight);
            if (radius != null)
                _pointLight.SetRadius(uid, (float)radius, pointLight);
            if (energy != null)
                _pointLight.SetEnergy(uid, (float)energy, pointLight);
            if (softness != null)
                _pointLight.SetSoftness(uid, (float)softness, pointLight);
        }

        // light bulbs burn your hands!
        if (TryComp<DamageOnInteractComponent>(uid, out var damageOnInteractComp))
            _damageOnInteractSystem.SetIsDamageActiveTo((uid, damageOnInteractComp), value);
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
        Dirty(uid, light);
        UpdateLight(uid, light);
    }

    private void OnDoAfter(EntityUid uid, PoweredLightComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        EjectBulb(args.Args.Target.Value, args.Args.User, component);

        args.Handled = true;
    }
}
