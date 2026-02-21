using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] private readonly SharedInternalsSystem _internals = default!;
    [Dependency] protected readonly SharedTransformSystem XformSystem = default!;

    private EntityQuery<InternalsComponent> _internalsQuery;

    /// <summary>
    /// The length to pre-allocate list/dicts of delta pressure entities on a <see cref="GridAtmosphereComponent"/>.
    /// </summary>
    public const int DeltaPressurePreAllocateLength = 1000;

    public override void Initialize()
    {
        base.Initialize();

        _internalsQuery = GetEntityQuery<InternalsComponent>();

        InitializeBreathTool();
        InitializeGases();
        InitializeCVars();
    }

    public GasPrototype GetGas(int gasId) => GasPrototypes[gasId];

    public GasPrototype GetGas(Gas gasId) => GasPrototypes[(int) gasId];

    public IEnumerable<GasPrototype> Gases => GasPrototypes;
}

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
    NumStates
}

/// <summary>
/// Data on the airtightness of a <see cref="TileAtmosphere"/>.
/// Cached on the <see cref="TileAtmosphere"/> and updated during
/// <see cref="AtmosphereSystem.ProcessRevalidate"/> if it was invalidated.
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
