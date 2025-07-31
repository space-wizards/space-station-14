using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// <para>System that handles <see cref="DeltaPressureComponent"/>.</para>
///
/// <para>Entities with a <see cref="DeltaPressureComponent"/> will take damage per atmostick
/// depending on the pressure they experience.</para>
///
/// <para>DeltaPressure logic is mostly handled in a partial class in Atmospherics.
/// This system handles the adding and removing of entities to a processing list,
/// as well as any field changes via the API.</para>
/// </summary>
public sealed class DeltaPressureSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeltaPressureComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DeltaPressureComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<DeltaPressureComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<DeltaPressureComponent, GridUidChangedEvent>(OnGridChanged);
    }

    private void OnComponentInit(Entity<DeltaPressureComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.AutoJoin)
        {
            TryAddToList(ent);
        }
    }

    private void OnComponentShutdown(Entity<DeltaPressureComponent> ent, ref ComponentShutdown args)
    {
        TryRemoveFromList(ent);
    }

    private void OnExamined(Entity<DeltaPressureComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsTakingDamage)
            args.PushMarkup(Loc.GetString("window-taking-damage"));
    }

    private void OnGridChanged(Entity<DeltaPressureComponent> ent, ref GridUidChangedEvent args)
    {
        if (args.OldGrid != null)
        {
            TryRemoveFromList(ent, args.OldGrid.Value);
        }

        if (args.NewGrid != null)
        {
            TryAddToList(ent, args.NewGrid.Value);
        }
    }

    /// <summary>
    /// Attempts to add an entity to the list of entities to do comparison and damage work on in AtmosphereSystem.
    /// </summary>
    /// <param name="ent">The entity to add.</param>
    /// <returns>True if the device was added, false if the device could not be added
    /// or was already in the processing list.</returns>
    [PublicAPI]
    public bool TryAddToList(Entity<DeltaPressureComponent> ent)
    {
        var xform = Transform(ent);

        // The entity is not on a grid, so it cannot possibly have an atmosphere that affects it.
        if (xform.GridUid == null)
        {
            return false;
        }

        return _atmosphereSystem.TryAddDeltaPressureEntity(xform.GridUid.Value, ent);
    }

    /// <summary>
    /// Attempts to add an entity to the list of entities to do comparison and damage work on in AtmosphereSystem.
    /// </summary>
    /// <param name="ent">The entity to add.</param>
    /// <param name="grid">The grid to add the entity to.</param>
    /// <returns>True if the device was added, false if the device could not be added
    /// or was already in the processing list.</returns>
    [PublicAPI]
    public bool TryAddToList(Entity<DeltaPressureComponent> ent, EntityUid grid)
    {
        return _atmosphereSystem.TryAddDeltaPressureEntity(grid, ent);
    }

    /// <summary>
    /// Attempts to remove an entity from the list of entities to do comparison and damage work on in AtmosphereSystem.
    /// </summary>
    /// <param name="ent">The entity to remove.</param>
    /// <returns>True if the device was removed, false if the device could not be removed
    /// or was not in the processing list.</returns>
    [PublicAPI]
    public bool TryRemoveFromList(Entity<DeltaPressureComponent> ent)
    {
        var xformEnt = Transform(ent);

        // The entity is not on a grid, so it cannot possibly have an atmosphere that affects it.
        if (xformEnt.GridUid == null)
        {
            return false;
        }

        return _atmosphereSystem.TryRemoveDeltaPressureEntity(xformEnt.GridUid.Value, ent);
    }

    /// <summary>
    /// Attempts to remove an entity from the list of entities to do comparison and damage work on in AtmosphereSystem.
    /// </summary>
    /// <param name="ent">The entity to remove.</param>
    /// <param name="grid">An EntityUid of the grid to remove this device from.</param>
    /// <returns>True if the device was removed, false if the device could not be removed
    /// from the provided grid EntityUid or was not in the processing list.</returns>
    [PublicAPI]
    public bool TryRemoveFromList(Entity<DeltaPressureComponent> ent, EntityUid grid)
    {
        return _atmosphereSystem.TryRemoveDeltaPressureEntity(grid, ent);
    }

    /// <summary>
    /// Does damage to an entity depending on the pressure experienced by it, based on the
    /// entity's <see cref="DeltaPressureComponent"/>.
    /// </summary>
    /// <param name="ent">The entity to apply damage to.</param>
    /// <param name="pressure">The absolute pressure being exerted on the entity.</param>
    /// <param name="deltaPressure">The delta pressure being exerted on the entity.</param>
    [PublicAPI]
    public void PerformDamage(Entity<DeltaPressureComponent> ent, float pressure, float deltaPressure)
    {
        var aboveMinPressure = pressure > ent.Comp.MinPressure;
        var aboveMinDeltaPressure = deltaPressure > ent.Comp.MinPressureDelta;
        if (!aboveMinPressure && !aboveMinDeltaPressure)
        {
            ent.Comp.IsTakingDamage = false;
            return;
        }

        // shitcode
        var baseDamage = ent.Comp.BaseDamage;
        var appliedDamage = ent.Comp.BaseDamage;
        if (aboveMinPressure)
        {
            appliedDamage = MutateDamage(ent, baseDamage, pressure - ent.Comp.MinPressure);
        }
        if (aboveMinDeltaPressure)
        {
            if (ent.Comp.StackDamage)
            {
                appliedDamage += MutateDamage(ent, appliedDamage, deltaPressure - ent.Comp.MinPressureDelta);
            }
            else
            {
                appliedDamage = MutateDamage(ent, baseDamage, deltaPressure - ent.Comp.MinPressureDelta);
            }
        }

        _damage.TryChangeDamage(ent, appliedDamage, interruptsDoAfters: false);
        ent.Comp.IsTakingDamage = true;
    }

    /// <summary>
    /// Mutates the damage dealt by a DamageSpecifier based on values on an entity with a DeltaPressureComponent.
    /// </summary>
    /// <param name="ent">The entity to base the manipulations off of (pull scaling type)</param>
    /// <param name="damage">The damage specifier to mutate.</param>
    /// <param name="pressure">The pressure being exerted on the entity.</param>
    /// <returns>The mutated DamageSpecifier.</returns>
    [PublicAPI]
    public static DamageSpecifier MutateDamage(Entity<DeltaPressureComponent> ent, DamageSpecifier damage, float pressure)
    {
        switch (ent.Comp.ScalingType)
        {
            case DeltaPressureDamageScalingType.Threshold:
                break;

            case DeltaPressureDamageScalingType.Linear:
                damage *= pressure * ent.Comp.ScalingPower;
                break;

            case DeltaPressureDamageScalingType.Log:
                // This little line's gonna cost us 51 CPU cycles
                damage *= Math.Log(pressure, ent.Comp.ScalingPower);
                break;

            case DeltaPressureDamageScalingType.Exponential:
                damage *= Math.Pow(pressure, ent.Comp.ScalingPower);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ent), "Invalid damage scaling type!");
        }

        return damage;
    }

    /// <summary>
    /// Sets the base damage applied to the entity per atmos tick when above the damage threshold.
    /// </summary>
    /// <param name="ent">The entity whose component to modify.</param>
    /// <param name="baseDamage">The new base damage specifier.</param>
    [PublicAPI]
    public void SetBaseDamage(Entity<DeltaPressureComponent> ent, DamageSpecifier baseDamage)
    {
        ent.Comp.BaseDamage = baseDamage;
    }

    /// <summary>
    /// Sets whether the entity stacks damage if both minimum pressure requirements are met.
    /// </summary>
    /// <param name="ent">The entity whose component to modify.</param>
    /// <param name="stackDamage">True to stack damage, false otherwise.</param>
    [PublicAPI]
    public void SetStackDamage(Entity<DeltaPressureComponent> ent, bool stackDamage)
    {
        ent.Comp.StackDamage = stackDamage;
    }

    /// <summary>
    /// Sets the minimum pressure in kPa at which the entity will start taking damage.
    /// </summary>
    /// <param name="ent">The entity whose component to modify.</param>
    /// <param name="minPressure">The new minimum pressure in kPa.</param>
    [PublicAPI]
    public void SetMinPressure(Entity<DeltaPressureComponent> ent, float minPressure)
    {
        ent.Comp.MinPressure = minPressure;
    }

    /// <summary>
    /// Sets the minimum difference in pressure between any side required for the entity to start taking damage.
    /// </summary>
    /// <param name="ent">The entity whose component to modify.</param>
    /// <param name="minPressureDelta">The new minimum pressure delta.</param>
    [PublicAPI]
    public void SetMinPressureDelta(Entity<DeltaPressureComponent> ent, float minPressureDelta)
    {
        ent.Comp.MinPressureDelta = minPressureDelta;
    }

    /// <summary>
    /// Sets the maximum pressure at which damage will no longer scale.
    /// </summary>
    /// <param name="ent">The entity whose component to modify.</param>
    /// <param name="maxPressure">The new maximum pressure.</param>
    [PublicAPI]
    public void SetMaxPressure(Entity<DeltaPressureComponent> ent, float maxPressure)
    {
        ent.Comp.MaxPressure = maxPressure;
    }

    /// <summary>
    /// Sets the scaling power constant for damage scaling behavior.
    /// </summary>
    /// <param name="ent">The entity whose component to modify.</param>
    /// <param name="scalingPower">The new scaling power.</param>
    [PublicAPI]
    public void SetScalingPower(Entity<DeltaPressureComponent> ent, float scalingPower)
    {
        ent.Comp.ScalingPower = scalingPower;
    }

    /// <summary>
    /// Sets the scaling type for damage calculation.
    /// </summary>
    /// <param name="ent">The entity whose component to modify.</param>
    /// <param name="scalingType">The new scaling type.</param>
    [PublicAPI]
    public void SetScalingType(Entity<DeltaPressureComponent> ent, DeltaPressureDamageScalingType scalingType)
    {
        ent.Comp.ScalingType = scalingType;
    }
}
