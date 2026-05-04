using Content.Shared.Atmos.Components;
using Content.Shared.CCVar;
using Content.Shared.Destructible;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Jittering;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems;

/// <summary>
/// This handles gas volumes that have a maximum pressure, and the destructive results of them exceeding that pressure.
/// You may call it the "MaxCapSystem" if you so desire.
/// </summary>
public abstract class GasMaxPressureSystem<T> : EntitySystem where T : IGasMaxPressureHolder, IComponent
{
    private float _maxExplosivePower;

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAtmosphereSystem Atmos = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedExplosionSystem _explosions = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, AtmosDeviceUpdateEvent>(OnDeviceUpdated);

        Subs.CVar(_cfg, CCVars.AtmosTankFragment, value => _maxExplosivePower = value, true);
    }

    private void OnDeviceUpdated(Entity<T> entity, ref AtmosDeviceUpdateEvent args)
    {
        // We don't update our atmos device if it's in the process of being deleted.
        if (CheckStatus(entity, args.dt))
            DeviceUpdated(entity, ref args);
    }

    /// <summary>
    /// Handler for our atmos device being updated.
    /// </summary>
    /// <param name="entity">Gas holding atmos device.</param>
    /// <param name="args"><see cref="AtmosDeviceUpdateEvent"/></param>
    protected abstract void DeviceUpdated(Entity<T> entity, ref AtmosDeviceUpdateEvent args);

    /// <summary>
    /// Handler for when this atmos device is about to break due to exceeding its maximum pressure too many times
    /// </summary>
    /// <param name="entity">Gas holding atmos device.</param>
    protected virtual void BeforeDeviceFailure(Entity<T> entity)
    {

    }

    /// <summary>
    /// Handler for when this atmos device loses integrity due to overpressure
    /// </summary>
    /// <param name="entity">Gas holding atmos device.</param>
    protected virtual void AfterDeviceFailure(Entity<T> entity)
    {

    }

    /// <summary>
    /// Handler for when this atmos device loses integrity due to overpressure
    /// </summary>
    /// <param name="entity">Gas holding atmos device.</param>
    protected virtual void IntegrityLost(Entity<T> entity)
    {

    }

    /// <summary>
    /// Handler for when this atmos device exceeds its safety parameters
    /// </summary>
    /// <param name="entity">Gas holding atmos device.</param>
    protected virtual void SafetyMeasures(Entity<T> entity)
    {

    }

    /// <summary>
    /// Checks the status of an atmos device that has a specified max pressure, and handles overpressure issues.
    /// </summary>
    /// <param name="entity">Gas holding atmos device.</param>
    /// <param name="dt">Time since the last status update.</param>
    /// <returns>True if the device hasn't failed. False if the device has failed and been destroyed.</returns>
    protected bool CheckStatus(Entity<T> entity, float dt)
    {
        var pressure = entity.Comp.Air.Pressure;

        // Better mixes mean bigger and faster explosions!
        if (pressure > entity.Comp.Overpressure * (entity.Comp.Integrity + 1))
        {
            Atmos.MergeContainingMixture(entity.Owner, entity.Comp.Air, excite: true);
            Audio.PlayPvs(entity.Comp.RuptureSound, Transform(entity).Coordinates, AudioParams.Default.WithVariation(0.125f));

            // Integrity failure, destroy ourselves!
            _destructible.DestroyEntity(entity);

            var totalIntensity = (float)Math.Sqrt(Atmos.GetOverPressure(entity.Comp.Air));
            if (_maxExplosivePower > 0 && _maxExplosivePower < totalIntensity)
                totalIntensity = _maxExplosivePower;

            _explosions.TriggerExplosive(entity, totalIntensity: totalIntensity);

            Dirty(entity);
            return false;
        }

        // Device begins to fail.
        if (pressure > entity.Comp.Overpressure)
        {
            IntegrityLost(entity);
            entity.Comp.Integrity -= dt;
            Appearance.SetData(entity.Owner, GasIntegrity.Integrity, entity.Comp.Integrity);
            Appearance.SetData(entity.Owner, GasIntegrity.MaxIntegrity, entity.Comp.MaxIntegrity);
        }
        else if (entity.Comp.Integrity < entity.Comp.MaxIntegrity)
        {
            entity.Comp.Integrity = Math.Min(entity.Comp.Integrity + dt, entity.Comp.MaxIntegrity);
            Appearance.SetData(entity.Owner, GasIntegrity.Integrity, entity.Comp.Integrity);
            Appearance.SetData(entity.Owner, GasIntegrity.MaxIntegrity, entity.Comp.MaxIntegrity);
        }

        // Device tries to prevent failure.
        if (pressure > entity.Comp.SafetyPressure)
            SafetyMeasures(entity);

        return true;
    }
}

[Serializable, NetSerializable, Flags]
public enum GasIntegrity
{
    Integrity,
    MaxIntegrity
}
