// Content.Shared/_Stargazer/SpiderLegs/SpiderGripSystem.cs
using System.Numerics;
using Content.Shared.Gravity;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared._Stargazer.SpiderLegs;

/// <summary>
/// Отменяет невесомость для <see cref="SpiderGripComponent"/>, пока в досягаемости ног есть станция.
/// Лежит в Shared: невесомость учитывается в предсказании движения на клиенте и на сервере.
/// Невесомость в движке кэшируется, поэтому хватку перепроверяем раз в CheckInterval
/// и пересчитываем невесомость, только когда она изменилась.
/// </summary>
public sealed partial class SpiderGripSystem : EntitySystem
{
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private List<Entity<MapGridComponent>> _grids = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpiderGripComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<SpiderGripComponent, ComponentStartup>(OnStartup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpiderGripComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var grip, out var xform))
        {
            grip.NextCheck -= frameTime;
            if (grip.NextCheck > 0f)
                continue;

            grip.NextCheck = grip.CheckInterval;

            var gripping = HasHold(grip, xform);
            if (gripping == grip.Gripping)
                continue;

            grip.Gripping = gripping;
            _gravity.RefreshWeightless(uid);
        }
    }

    private void OnIsWeightless(Entity<SpiderGripComponent> ent, ref IsWeightlessEvent args)
    {
        if (args.Handled || !ent.Comp.Gripping)
            return;

        args.IsWeightless = false;
        args.Handled = true;
    }

    private void OnStartup(Entity<SpiderGripComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<GravityAffectedComponent>(ent);
        ent.Comp.Gripping = HasHold(ent.Comp, Transform(ent));
        _gravity.RefreshWeightless(ent.Owner);
    }

    /// <summary>
    /// Есть ли станция в досягаемости ног: ближайшая КРОМКА непустой клетки какого-нибудь грида
    /// не дальше GripRange от центра. Ноги на клиенте цепляются по той же мерке.
    /// </summary>
    private bool HasHold(SpiderGripComponent grip, TransformComponent xform)
    {
        var coords = _xform.GetMapCoordinates(xform);
        if (coords.MapId == MapId.Nullspace)
            return false;

        var range = grip.GripRange;
        var box = Box2.CenteredAround(coords.Position, new Vector2(range * 2f + 2f, range * 2f + 2f));

        _grids.Clear();
        _map.FindGridsIntersecting(coords.MapId, box, ref _grids);

        foreach (var (gridUid, gridComp) in _grids)
        {
            var size = gridComp.TileSize;
            var local = _map.WorldToLocal(gridUid, gridComp, coords.Position);

            // Перечисление по центрам клеток — берём с запасом на полдиагонали клетки,
            // а точное расстояние до кромки считаем сами.
            var circle = new Circle(coords.Position, range + size * 0.75f);
            var tiles = _map.GetTilesIntersecting(gridUid, gridComp, circle);
            while (tiles.MoveNext())
            {
                var idx = tiles.Current.GridIndices;
                var min = new Vector2(idx.X * size, idx.Y * size);
                var nearest = Vector2.Clamp(local, min, min + new Vector2(size, size));
                if (Vector2.DistanceSquared(local, nearest) <= range * range)
                    return true;
            }
        }

        return false;
    }
}
