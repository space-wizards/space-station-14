using System.Numerics;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Cargo;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems;

[UsedImplicitly]
public sealed class GasTankSystem : SharedGasTankSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private const float MinimumSoundValvePressure = 21.3f; // Arbitrary number

    private const float ReleaseArea = 0.0005f; // About 5cm^2

    // A vector bias for throwing our gas tanks in radians. Averages about -43 degrees since the sprite is at a 45-degree angle.
    private static readonly Vector2 ThrowVector = new (-1.0f, -0.5f);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasTankComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<GasTankComponent, GasAnalyzerScanEvent>(OnAnalyzed);
        SubscribeLocalEvent<GasTankComponent, PriceCalculationEvent>(OnGasTankPrice);
    }

    protected override void DeviceUpdated(Entity<GasTankComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        // Release gas if valve is open
        // Disconnect from internals if valve is open
        if (entity.Comp.ReleaseValveOpen)
        {
            DisconnectFromInternals(entity);
            ReleaseGas(entity, args.dt);
        }
        else if (entity.Comp.CheckUser)
        {
            entity.Comp.CheckUser = false;
            if (Transform(entity).ParentUid != entity.Comp.User)
            {
                DisconnectFromInternals(entity);
            }
        }

        Atmos.React(entity.Comp.Air, entity.Comp);

        if ((entity.Comp.IsConnected || entity.Comp.ReleaseValveOpen) && UI.IsUiOpen(entity.Owner, SharedGasTankUiKey.Key))
            UpdateUserInterface(entity);
    }

    public override void UpdateUserInterface(Entity<GasTankComponent> ent)
    {
        var (owner, component) = ent;
        UI.SetUiState(owner,
            SharedGasTankUiKey.Key,
            new GasTankBoundUserInterfaceState
            {
                TankPressure = component.Air.Pressure
            });
    }

    private void OnParentChange(EntityUid uid, GasTankComponent component, ref EntParentChangedMessage args)
    {
        // When an item is moved from hands -> pockets, the container removal briefly dumps the item on the floor.
        // So this is a shitty fix, where the parent check is just delayed. But this really needs to get fixed
        // properly at some point.
        component.CheckUser = true;
    }

    /// <summary>
    /// Tries to release gas through the pressure release valve.
    /// </summary>
    /// <param name="entity">The gas tank entity releasing gas</param>
    /// <param name="dt">The amount of time since the last update</param>
    /// <returns></returns>
    private void ReleaseGas(Entity<GasTankComponent> entity, float dt)
    {
        var environment = _atmosphereSystem.GetContainingMixture(entity.Owner, false, true);

        var deltaP = environment == null
            ? entity.Comp.Air.Pressure
            : entity.Comp.Air.Pressure - environment.Pressure;

        // Cap deltaP by the maximum output pressure of the tank.
        if (deltaP < entity.Comp.SafetyPressure)
            deltaP = Math.Min(entity.Comp.ReleasePressure, deltaP);

        var removed = _atmosphereSystem.FlowGas(entity.Comp.Air, deltaP, dt, ReleaseArea);

        if (removed == null)
            return;

        if (environment != null)
            _atmosphereSystem.Merge(environment, removed);

        // If we wouldn't produce a sound, don't throw or play a sound.
        if (removed.Pressure < MinimumSoundValvePressure)
            return;

        Audio.PlayPvs(entity.Comp.ReleaseSound, entity);

        var strength = Atmos.GetOverPressure(removed) * Atmospherics.kPaToKg_m2;

        if (strength <= 0)
            return;

        // TODO: I hate throwing system. I shouldn't need to do this boilerplate to get a nice looking throw
        var rot = _xform.GetWorldRotation(entity);
        var ang = _random.NextAngle(rot + ThrowVector.X, rot + ThrowVector.Y);

        // We bias by angle to make sure it doesn't rotate too much and flies relatively straight.
        _physics.ApplyAngularImpulse(entity, (float)(strength * ang));

        // TODO ATMOS: If we can predict ReleaseGas at some point, we should have this apply an impulse to a person holding this gas tank.
        _throwing.TryThrow(entity, ang.ToWorldVec() * strength, strength, doSpin: false);
    }

    public GasMixture RemoveAirOutput(Entity<GasTankComponent> gasTank, float volume)
    {
        var mixture = _atmosphereSystem.RemoveVolumeAtPressure(gasTank.Comp.Air, volume, gasTank.Comp.ReleasePressure);
        // We resize the volume because lungs breathe in volume rather than being pressure based atm.
        // If we don't do this, they won't consume all of the outputted gas or will consume way too much.
        mixture.Volume = volume;
        return mixture;
    }

    public GasMixture RemoveAir(Entity<GasTankComponent> gasTank, float amount)
    {
        return gasTank.Comp.Air.Remove(amount);
    }

    protected override void SafetyMeasures(Entity<GasTankComponent> entity)
    {
        if (entity.Comp.ReleaseValveOpen)
            return;

        ToggleValve(entity);
        if (entity.Comp.SafetyAlert != null)
            _popup.PopupEntity(Loc.GetString(entity.Comp.SafetyAlert), entity, PopupType.LargeCaution);

        Dirty(entity);
    }

    /// <summary>
    /// Returns the gas mixture for the gas analyzer
    /// </summary>
    private void OnAnalyzed(EntityUid uid, GasTankComponent component, GasAnalyzerScanEvent args)
    {
        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(uid), component.Air));
    }

    private void OnGasTankPrice(EntityUid uid, GasTankComponent component, ref PriceCalculationEvent args)
    {
        args.Price += _atmosphereSystem.GetPrice(component.Air);
    }
}
