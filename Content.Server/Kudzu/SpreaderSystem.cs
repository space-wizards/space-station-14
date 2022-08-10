using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Kudzu;

// Future work includes making the growths per interval thing not global, but instead per "group"
public sealed class SpreaderSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <summary>
    /// Maximum number of edges that can grow out every interval.
    /// </summary>
    private const int GrowthsPerInterval = 1;

    private float _accumulatedFrameTime = 0.0f;

    private readonly HashSet<EntityUid> _edgeGrowths = new ();

    public override void Initialize()
    {
        SubscribeLocalEvent<SpreaderComponent, ComponentAdd>(SpreaderAddHandler);
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);
    }

    private void OnAirtightChanged(AirtightChanged e)
    {
        UpdateNearbySpreaders((e.Airtight).Owner, e.Airtight);
    }

    private void SpreaderAddHandler(EntityUid uid, SpreaderComponent component, ComponentAdd args)
    {
        if (component.Enabled)
            _edgeGrowths.Add(uid); // ez
    }

    public void UpdateNearbySpreaders(EntityUid blocker, AirtightComponent comp)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(blocker, out var transform))
            return; // how did we get here?

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid)) return;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            if (!comp.AirBlockedDirection.IsFlagSet(direction)) continue;

            foreach (var ent in grid.GetInDir(transform.Coordinates, direction.ToDirection()))
            {
                if (EntityManager.TryGetComponent<SpreaderComponent>(ent, out var s) && s.Enabled)
                    _edgeGrowths.Add(ent);
            }
        }
    }

    public override void Update(float frameTime)
    {
        _accumulatedFrameTime += frameTime;

        if (!(_accumulatedFrameTime >= 1.0f))
            return;

        _accumulatedFrameTime -= 1.0f;

        var growthList = _edgeGrowths.ToList();
        _robustRandom.Shuffle(growthList);

        var successes = 0;
        foreach (var entity in growthList)
        {
            if (!TryGrow(entity)) continue;

            successes += 1;
            if (successes >= GrowthsPerInterval)
                break;
        }
    }

    private bool TryGrow(EntityUid ent, TransformComponent? transform = null, SpreaderComponent? spreader = null)
    {
        if (!Resolve(ent, ref transform, ref spreader, false))
            return false;

        if (spreader.Enabled == false) return false;

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid)) return false;

        var didGrow = false;

        for (var i = 0; i < 4; i++)
        {
            var direction = (DirectionFlag) (1 << i);
            var coords = transform.Coordinates.Offset(direction.AsDir().ToVec());
            if (grid.GetTileRef(coords).Tile.IsEmpty || _robustRandom.Prob(1 - spreader.Chance)) continue;
            var ents = grid.GetLocal(coords);

            if (ents.Any(x => IsTileBlockedFrom(x, direction))) continue;

            // Ok, spawn a plant
            didGrow = true;
            EntityManager.SpawnEntity(spreader.GrowthResult, transform.Coordinates.Offset(direction.AsDir().ToVec()));
        }

        return didGrow;
    }

    public void EnableSpreader(EntityUid ent, SpreaderComponent? component = null)
    {
        if (!Resolve(ent, ref component))
            return;
        component.Enabled = true;
        _edgeGrowths.Add(ent);
    }

    private bool IsTileBlockedFrom(EntityUid ent, DirectionFlag dir)
    {
        if (EntityManager.TryGetComponent<SpreaderComponent>(ent, out _))
            return true;

        if (!EntityManager.TryGetComponent<AirtightComponent>(ent, out var airtight))
            return false;

        var oppositeDir = dir.AsDir().GetOpposite().ToAtmosDirection();

        return airtight.AirBlocked && airtight.AirBlockedDirection.IsFlagSet(oppositeDir);
    }
}
