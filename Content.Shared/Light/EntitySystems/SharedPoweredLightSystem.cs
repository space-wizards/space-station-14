using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Light.EntitySystems;

public abstract class SharedPoweredLightSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private   readonly DamageOnInteractSystem _damageOnInteractSystem = default!;
    [Dependency] private   readonly SharedAmbientSoundSystem _ambientSystem = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] private   readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private   readonly SharedLightBulbSystem _bulbSystem = default!;
    [Dependency] private   readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private   readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private   readonly SharedPointLightSystem _pointLight = default!;

    private static readonly TimeSpan ThunkDelay = TimeSpan.FromSeconds(2);
    public const string LightBulbContainer = "light_bulb";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoweredLightComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PoweredLightComponent, InteractHandEvent>(OnInteractHand);


        SubscribeLocalEvent<PoweredLightComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<PoweredLightComponent, PoweredLightDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PoweredLightComponent, DamageChangedEvent>(HandleLightDamaged);
    }

    private void OnInteractUsing(EntityUid uid, Components.PoweredLightComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = InsertBulb(uid, args.Used, component);
    }

    private void OnInteractHand(EntityUid uid, Components.PoweredLightComponent light, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        // check if light has bulb to eject
        var bulbUid = GetBulb(uid, light);
        if (bulbUid == null)
            return;

        var userUid = args.User;
        //removing a broken/burned bulb, so allow instant removal
        if(TryComp<LightBulbComponent>(bulbUid.Value, out var bulb) && bulb.State != LightBulbState.Normal)
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
        if (!_containerSystem.Insert(bulbUid, light.LightBulbContainer))
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

        return light.LightBulbContainer.ContainedEntity;
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
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref light, false))
            return;

        if (!_receiver.ResolveApc(uid, ref powerReceiver))
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
                    var time = GameTiming.CurTime;
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

        if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            return;

        _appearance.SetData(uid, PoweredLightVisuals.Blinking, isNowBlinking, appearance);
    }

    private void SetLight(EntityUid uid, bool value, Color? color = null, PoweredLightComponent? light = null, float? radius = null, float? energy = null, float? softness = null)
    {
        if (!Resolve(uid, ref light))
            return;

        light.CurrentLit = value;
        _ambientSystem.SetAmbience(uid, value);

        if (_pointLight.TryGetLight(uid, out var pointLight))
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

    private void OnDoAfter(EntityUid uid, PoweredLightComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        EjectBulb(args.Args.Target.Value, args.Args.User, component);

        args.Handled = true;
    }
}
