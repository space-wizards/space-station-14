using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] protected IPrototypeManager ProtoMan = default!;
    [Dependency] private SharedInternalsSystem _internals = default!;
    [Dependency] protected SharedTransformSystem XformSystem = default!;

    [Dependency] private EntityQuery<InternalsComponent> _internalsQuery = default!;

    /// <summary>
    /// The length to pre-allocate list/dicts of delta pressure entities on a <see cref="GridAtmosphereComponent"/>.
    /// </summary>
    public const int DeltaPressurePreAllocateLength = 1000;

    public override void Initialize()
    {
        base.Initialize();

        InitializeBreathTool();
        InitializeGases();
        InitializeCVars();
    }

    /// <summary>
    /// Gets the <see cref="GasPrototype"/> of a given gas ID.
    /// </summary>
    /// <param name="gasId">The gas ID to get the prototype of.</param>
    /// <returns>The <see cref="GasPrototype"/> of the given gas ID.</returns>
    public GasPrototype GetGas(int gasId)
    {
        return GasPrototypes[gasId];
    }

    /// <summary>
    /// Gets the <see cref="GasPrototype"/> of a given gas ID.
    /// </summary>
    /// <param name="gasId">The gas ID to get the prototype of.</param>
    /// <returns>The <see cref="GasPrototype"/> of the given gas ID.</returns>
    public GasPrototype GetGas(Gas gasId)
    {
        return GasPrototypes[(int)gasId];
    }

    /// <summary>
    /// Gets an enumerable of all the <see cref="GasPrototype"/>s.
    /// </summary>
    public IEnumerable<GasPrototype> Gases => GasPrototypes;
}

/// <summary>
/// Enum that represents the current processing state of a
/// <see cref="GridAtmosphereComponent"/>.
/// </summary>
public enum AtmosphereProcessingState : byte
{
    Revalidate,
    TileEqualize,
    ActiveTiles,
    ExcitedGroups,
    HighPressureDelta,
    DeltaPressure,
    Hotspots,
    Superconductivity,
    PipeNet,
    AtmosDevices,
    NumStates,
}

/// <summary>
/// Snapshot of optional-phase CVars taken at cycle start, so mid-cycle flips don't apply until next cycle.
/// </summary>
[Flags]
public enum AtmosPhaseFlags : byte
{
    None = 0,
    MonstermosEqualization = 1 << 0,
    ExcitedGroups = 1 << 1,
    DeltaPressureDamage = 1 << 2,
    Superconduction = 1 << 3,
}

/// <summary>
/// In-flight cycle position and phase flag snapshot.
/// </summary>
public record struct AtmosphereCycleCursor(AtmosphereProcessingState Phase, AtmosPhaseFlags Flags);

/// <summary>
/// Data on the airtightness of a <see cref="TileAtmosphere"/>.
/// Cached on the <see cref="TileAtmosphere"/> and updated during
/// <see cref="AtmosphereProcessingState.Revalidate"/> if it was invalidated.
/// </summary>
/// <param name="BlockedDirections">The current directions blocked on this tile.
/// This is where air cannot flow to.</param>
/// <param name="NoAirWhenBlocked">Whether the tile can have air when blocking directions.
/// Common for entities like thin windows which only block one face but can still have air in the residing tile.</param>
/// <param name="FixVacuum">If true, Atmospherics will generate air (yes, creating matter from nothing)
/// using the adjacent tiles as a seed if the airtightness is removed and the tile has no air.
/// This allows stuff like airlocks that void air when becoming airtight to keep opening/closing without
/// draining a room by continuously voiding air.</param>
public readonly record struct AirtightData(
    AtmosDirection BlockedDirections,
    bool NoAirWhenBlocked,
    bool FixVacuum);

/// <summary>
/// Struct that holds the result of delta pressure damage processing for an entity.
/// This is only created and enqueued when the entity needs to take damage.
/// </summary>
/// <param name="Ent">The entity to deal damage to.</param>
/// <param name="Pressure">The current absolute pressure the entity is experiencing.</param>
/// <param name="DeltaPressure">The current delta pressure the entity is experiencing.</param>
public readonly record struct DeltaPressureDamageResult(
    Entity<DeltaPressureComponent> Ent,
    float Pressure,
    float DeltaPressure);
