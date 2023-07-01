using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Dataset;
using Content.Shared.Salvage;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._FTL.FTLPoints;

/// <summary>
/// This handles the generation of FTL points
/// </summary>
public sealed class FTLPointsSystem : EntitySystem
{
    [Dependency] private EntityManager _entManager = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private MetaDataSystem _metaDataSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private readonly ShuttleConsoleSystem _consoleSystem = default!;

    public const int PREFERRED_POINT_AMOUNT = 3;

    public void RegeneratePoints()
    {
        ClearDisposablePoints();

        for (int i = 0; i < PREFERRED_POINT_AMOUNT; i++)
        {
            GenerateDisposablePoint();
        }

        Log.Debug("Regenerated points.");
    }

    /// <summary>
    /// Clears all disposable points
    /// </summary>
    public void ClearDisposablePoints()
    {
        var query = EntityQueryEnumerator<DisposalFTLPointComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            DeletePoint(uid);
        }
    }

    public void DeletePoint(EntityUid point)
    {
        Del(point);
    }

    /// <summary>
    /// Generates a temporary disposable FTL point.
    /// </summary>
    public void GenerateDisposablePoint()
    {
        var mapId = _mapManager.CreateMap();
        var mapUid = _mapManager.GetMapEntityId(mapId);
        _metaDataSystem.SetEntityName(mapUid,
            SharedSalvageSystem.GetFTLName(_prototypeManager.Index<DatasetPrototype>("names_borer"), _random.Next(0,1000000)));

        EnsureComp<FTLDestinationComponent>(_mapManager.GetMapEntityId(mapId));
        AddComp<DisposalFTLPointComponent>(mapUid);
        _consoleSystem.RefreshShuttleConsoles();
    }
}
