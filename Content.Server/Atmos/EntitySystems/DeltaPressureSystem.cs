using Content.Server.Atmos.Components;
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
            args.PushMarkup("The object is buckling inwards!");
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

    // TODO: API for setting fields
}
