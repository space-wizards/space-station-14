using Content.Server.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;

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
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeltaPressureComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DeltaPressureComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<DeltaPressureComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DeltaPressureComponent, MoveEvent>(OnMoveEvent);

        SubscribeLocalEvent<DeltaPressureComponent, GridUidChangedEvent>(OnGridChanged);
    }

    private void OnMoveEvent(Entity<DeltaPressureComponent> ent, ref MoveEvent args)
    {
        var xform = Transform(ent);
        // May move off-grid, so, might as well protect against that.
        if (!TryComp<MapGridComponent>(xform.GridUid, out var mapGridComponent))
        {
            return;
        }

        ent.Comp.CurrentPosition = _map.CoordinatesToTile(ent.Owner, mapGridComponent, args.NewPosition);
    }

    private void OnComponentInit(Entity<DeltaPressureComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.AutoJoinProcessingList)
        {
            TryAddToProcessingQueue(ent);
        }
    }

    private void OnComponentShutdown(Entity<DeltaPressureComponent> ent, ref ComponentShutdown args)
    {
        TryRemoveFromProcessingQueue(ent);
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
            TryRemoveFromProcessingQueue(ent, args.OldGrid.Value);
        }

        if (args.NewGrid != null)
        {
            TryAddToProcessingQueue(ent, args.NewGrid.Value);
        }
    }

    /// <summary>
    /// Attempts to add an entity to the list of entities to do comparison and damage work on in AtmosphereSystem.
    /// </summary>
    /// <param name="ent">The entity to add.</param>
    /// <returns>True if the device was added, false if the device could not be added
    /// or was already in the processing list.</returns>
    [PublicAPI]
    public bool TryAddToProcessingQueue(Entity<DeltaPressureComponent> ent)
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
    public bool TryAddToProcessingQueue(Entity<DeltaPressureComponent> ent, EntityUid grid)
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
    public bool TryRemoveFromProcessingQueue(Entity<DeltaPressureComponent> ent)
    {
        return ent.Comp.GridUid != null && _atmosphereSystem.TryRemoveDeltaPressureEntity(ent.Comp.GridUid.Value, ent);
    }

    /// <summary>
    /// Attempts to remove an entity from the list of entities to do comparison and damage work on in AtmosphereSystem.
    /// </summary>
    /// <param name="ent">The entity to remove.</param>
    /// <param name="grid">An EntityUid of the grid to remove this device from.</param>
    /// <returns>True if the device was removed, false if the device could not be removed
    /// from the provided grid EntityUid or was not in the processing list.</returns>
    [PublicAPI]
    public bool TryRemoveFromProcessingQueue(Entity<DeltaPressureComponent> ent, EntityUid grid)
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
    /// <param name="aboveMinPressure">If the entity is currently above the minimum pressure.</param>
    /// <param name="aboveMinDeltaPressure">If the entity is currently above the minimum delta pressure.</param>
    [PublicAPI]
    public void PerformDamage(Entity<DeltaPressureComponent> ent, float pressure, float deltaPressure, bool aboveMinPressure, bool aboveMinDeltaPressure)
    {
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

        _damage.TryChangeDamage(ent, appliedDamage, ignoreResistances: true, interruptsDoAfters: false);
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
}
