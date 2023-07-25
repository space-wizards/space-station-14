using Content.Server._FTL.FTLPoints.Components;
using Content.Server._FTL.FTLPoints.Effects;
using Content.Server._FTL.FTLPoints.Events;
using Content.Server._FTL.FTLPoints.Prototypes;
using Content.Server.Parallax;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Dataset;
using Content.Shared.Parallax;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._FTL.FTLPoints.Systems;

/// <summary>
/// This handles the generation of FTL points
/// </summary>
public sealed partial class FTLPointsSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ShuttleConsoleSystem _consoleSystem = default!;

    public void RegeneratePoints()
    {
        ClearDisposablePoints();

        var preferredPointAmount = _random.Next(2, 5);

        for (var i = 0; i < preferredPointAmount; i++)
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
    public void GenerateDisposablePoint(FTLPointPrototype? prototype = null)
    {
        var picked = _prototypeManager.Index<WeightedRandomPrototype>("FTLPoints").Pick();

        Log.Info($"Picked {picked} as point type.");

        var point = _prototypeManager.Index<FTLPointPrototype>(picked);

        if (prototype != null)
            point = prototype;

        // create map
        if (point.OverrideSpawn == null)
        {
            if (!_random.Prob(point.Probability))
                return;

            var mapId = _mapManager.CreateMap();
            _mapManager.AddUninitializedMap(mapId);
            var mapUid = _mapManager.GetMapEntityId(mapId);

            // make it ftlable
            EnsureComp<FTLDestinationComponent>(mapUid);
            EnsureComp<DisposalFTLPointComponent>(mapUid);
            _metaDataSystem.SetEntityName(mapUid, $"[{Loc.GetString(point.Tag)}] {
                SharedSalvageSystem.GetFTLName(_prototypeManager.Index<DatasetPrototype>("names_borer"), _random.Next())}");
            _consoleSystem.RefreshShuttleConsoles();

            // add parallax
            var parallaxes = new[]
            {
                "AspidParallax",
                "KettleStation",
                "Default",
                "Blank",
                "BagelStation",
                "Blue_Nebula_01",
                "Blue_Nebula_02",
                "Blue_Nebula_03",
                "Blue_Nebula_04",
                "Green_Nebula_01",
                "Green_Nebula_02",
                "Green_Nebula_03",
                "Green_Nebula_04",
                "Green_Nebula_06",
                "Green_Nebula_07",
                "Green_Nebula_08",
                "Purple_Nebula_01",
                "Purple_Nebula_02",
                "Purple_Nebula_03",
                "Purple_Nebula_04",
                "Purple_Nebula_05",
                "Purple_Nebula_08"
            };
            var parallax = EnsureComp<ParallaxComponent>(mapUid);
            parallax.Parallax = _random.Pick(parallaxes);

            // spawn the stuff
            foreach (var effect in point.FtlPointEffects)
            {
                if (_random.Prob(effect.Probability))
                {
                    effect.Effect(new FTLPointEffect.FTLPointEffectArgs(mapUid, mapId, _entManager, _mapManager));
                }
            }

        }
        else
        {
            point.OverrideSpawn.Effect(new FTLPointSpawn.FTLPointSpawnArgs(_entManager, _mapManager));
        }
    }
}
