using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server._FTL.FTLPoints.Components;
using JetBrains.Annotations;
using Robust.Server.GameStates;
using Robust.Shared.Map;

namespace Content.Server._FTL.FTLPoints.Systems;

/// <summary>
/// This handles managing the starmap singleton, such as getting stars in range, and other stuff.
/// </summary>
public sealed partial class FTLPointsSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StarMapComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(EntityUid uid, StarMapComponent component, ComponentStartup args)
    {
        _pvs.AddGlobalOverride(uid);
    }

    // ew i know singletons suck but this is the only thing that makes sense
    public bool TryGetStarMap([NotNullWhen(true)] ref StarMapComponent? component)
    {
        if (component != null)
            return true;

        var query = EntityQuery<StarMapComponent>().ToList();
        component = !query.Any() ? CreatePointManager() : query.First();
        return true;
    }

    private StarMapComponent CreatePointManager()
    {
        var manager = Spawn(null, MapCoordinates.Nullspace);
        return EnsureComp<StarMapComponent>(manager);
    }

    #region Public API

    [PublicAPI]
    public List<EntityUid>? GetStarsInRange(Vector2 position, float range, StarMapComponent? component = null)
    {
        if (!TryGetStarMap(ref component))
            return default;

        var list = new List<EntityUid>();
        foreach (var (starPos, starUid) in component.StarMap)
        {
            if (Vector2.Distance(position, starPos) <= range)
                list.Add(starUid);
        }

        return list;
    }

    [PublicAPI]
    public bool TryAddPoint(EntityUid uid, Vector2 position, StarMapComponent? component = null)
    {
        return TryGetStarMap(ref component) && component.StarMap.TryAdd(position, uid);
    }

    #endregion
}
