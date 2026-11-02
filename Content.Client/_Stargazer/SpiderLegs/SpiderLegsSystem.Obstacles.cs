// Content.Client/_Stargazer/SpiderLegs/SpiderLegsSystem.Obstacles.cs
using System.Numerics;
using Content.Shared._Stargazer.SpiderLegs;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Client._Stargazer.SpiderLegs;

/// <summary>Что находится в клетке под точкой.</summary>
internal enum SpiderCell : byte
{
    Floor,
    Wall,
    Space,
}

/// <summary>
/// Обход стен: стопа не ставится в клетку, где стоит заякоренная сущность
/// с "высоким" слоем коллизии (стены, окна, закрытые двери), и по желанию — в космос.
/// Проверяются только заякоренные сущности клетки, без физических запросов,
/// а результаты кэшируются на кадр, так что это дёшево.
/// </summary>
public sealed partial class SpiderLegsSystem
{
    [Dependency] private SharedMapSystem _map = default!;

    // Направления, в которых ищется свободное место, если желаемая точка в стене (градусы → радианы).
    private static readonly float[] ProbeAngles =
    {
        0f,
        MathF.PI / 9f, -MathF.PI / 9f,               // ±20°
        2f * MathF.PI / 9f, -2f * MathF.PI / 9f,     // ±40°
        MathF.PI / 3f, -MathF.PI / 3f,               // ±60°
        4f * MathF.PI / 9f, -4f * MathF.PI / 9f,     // ±80°
        5f * MathF.PI / 9f, -5f * MathF.PI / 9f,     // ±100°
    };

    // Штраф за отклонение от желаемого направления (тайлов на радиан).
    private const float AnglePenalty = 0.9f;

    // Направления, в которых нога в космосе ищет кромку корпуса: 0, ±20°, ... ±180°.
    private static readonly float[] HullProbeAngles = BuildHullAngles();

    // Шаг луча в поиске кромки корпуса, тайлов.
    private const float HullMarchStep = 0.1f;

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    // Контекст текущей сущности.
    private bool _obstaclesActive;
    // Родитель — грид: координаты ног = координаты тайлов этого грида.
    private EntityUid _obstacleGrid;
    private MapGridComponent? _obstacleGridComp;
    // Родитель — карта (кресло в открытом космосе): ближайшие гриды и их обратные матрицы.
    private readonly List<Entity<MapGridComponent>> _nearGrids = new();
    private readonly List<Matrix3x2> _nearGridsInv = new();
    private int _obstacleMask;
    private bool _obstacleBlockSpace;
    private readonly Dictionary<(EntityUid, Vector2i), SpiderCell> _cellCache = new();

    private bool ObstaclesActive => _obstaclesActive;

    private void InitializeObstacles()
    {
        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    private static float[] BuildHullAngles()
    {
        var list = new List<float> { 0f };
        for (var deg = 20; deg <= 180; deg += 20)
        {
            list.Add(MathF.PI * deg / 180f);
            list.Add(-MathF.PI * deg / 180f);
        }

        return list.ToArray();
    }

    /// <summary>Готовит проверку препятствий для одной сущности на этот кадр.</summary>
    private void BeginObstacleQuery(SpiderLegsComponent comp, TransformComponent xform)
    {
        _cellCache.Clear();
        _nearGrids.Clear();
        _nearGridsInv.Clear();
        _obstacleGridComp = null;
        _obstaclesActive = false;

        if (!comp.AvoidWalls)
            return;

        _obstacleMask = (int) comp.BlockMask;
        _obstacleBlockSpace = comp.BlockSpace;

        if (xform.GridUid is { } grid && grid == xform.ParentUid)
        {
            // Родитель — грид: координаты ног совпадают с координатами его тайлов.
            if (!TryComp(grid, out MapGridComponent? gridComp))
                return;

            _obstacleGrid = grid;
            _obstacleGridComp = gridComp;
            _obstaclesActive = true;

            // Корпус сам стоит внутри стены — лучи от него никуда не пройдут, в этом кадре стены не учитываем.
            if (Classify(xform.LocalPosition) == SpiderCell.Wall)
            {
                _obstacleGridComp = null;
                _obstaclesActive = false;
            }

            return;
        }

        if (xform.MapUid is { } map && map == xform.ParentUid)
        {
            // Открытый космос: координаты ног — мировые. Ищем станции в досягаемости ног.
            var pos = xform.LocalPosition;
            var range = comp.MaxReach + 1f;
            var box = Box2.CenteredAround(pos, new Vector2(range * 2f, range * 2f));
            var grids = _nearGrids;
            _map.FindGridsIntersecting(xform.MapID, box, ref grids);
            foreach (var g in _nearGrids)
            {
                _nearGridsInv.Add(_xform.GetInvWorldMatrix(g.Owner));
            }

            _obstaclesActive = true;
        }
    }

    private Vector2i TileOf(Vector2 p)
    {
        var size = _obstacleGridComp!.TileSize;
        return new Vector2i((int) MathF.Floor(p.X / size), (int) MathF.Floor(p.Y / size));
    }

    /// <summary>Что в точке (координаты родителя): пол, стена или космос.</summary>
    private SpiderCell Classify(Vector2 p)
    {
        if (!_obstaclesActive)
            return SpiderCell.Floor;

        if (_obstacleGridComp != null)
            return CellOf(_obstacleGrid, _obstacleGridComp, TileOf(p));

        // Кресло в космосе: точка в мировых координатах, проверяем ближайшие гриды.
        for (var i = 0; i < _nearGrids.Count; i++)
        {
            var (gridUid, gridComp) = _nearGrids[i];
            var local = Vector2.Transform(p, _nearGridsInv[i]);
            var size = gridComp.TileSize;
            var idx = new Vector2i((int) MathF.Floor(local.X / size), (int) MathF.Floor(local.Y / size));
            var cell = CellOf(gridUid, gridComp, idx);
            if (cell != SpiderCell.Space)
                return cell;
        }

        return SpiderCell.Space;
    }

    /// <summary>Стоит ли в клетке под точкой препятствие для стопы на полу.</summary>
    private bool IsBlocked(Vector2 p)
    {
        return IsBlockedCell(Classify(p));
    }

    /// <summary>То же по индексу тайла родительского грида (для DDA-луча).</summary>
    private bool IsTileBlocked(Vector2i idx)
    {
        if (_obstacleGridComp == null)
            return false;

        return IsBlockedCell(CellOf(_obstacleGrid, _obstacleGridComp, idx));
    }

    private bool IsBlockedCell(SpiderCell cell)
    {
        return cell == SpiderCell.Wall || (_obstacleBlockSpace && cell == SpiderCell.Space);
    }

    private SpiderCell CellOf(EntityUid gridUid, MapGridComponent gridComp, Vector2i idx)
    {
        if (_cellCache.TryGetValue((gridUid, idx), out var cell))
            return cell;

        cell = ComputeCell(gridUid, gridComp, idx);
        _cellCache[(gridUid, idx)] = cell;
        return cell;
    }

    private SpiderCell ComputeCell(EntityUid gridUid, MapGridComponent gridComp, Vector2i idx)
    {
        if (!_map.TryGetTileRef(gridUid, gridComp, idx, out var tileRef) || tileRef.Tile.IsEmpty)
            return SpiderCell.Space;

        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, gridComp, idx);
        while (anchored.MoveNext(out var ent))
        {
            // Открытая дверь отключает коллизию — такую клетку считаем свободной.
            if (!_physicsQuery.TryGetComponent(ent.Value, out var body) || !body.CanCollide)
                continue;

            if (!_fixturesQuery.TryGetComponent(ent.Value, out var fixtures))
                continue;

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (fixture.Hard && (fixture.CollisionLayer & _obstacleMask) != 0)
                    return SpiderCell.Wall;
            }
        }

        return SpiderCell.Floor;
    }

    /// <summary>
    /// Корпус висит над космосом: ищем лучами из центра корпуса ближайшую кромку станции
    /// (первую не-космос клетку) не дальше hullRange от центра корпуса и в досягаемости ноги.
    /// hullRange совпадает с радиусом хватки в геймплее, поэтому "нога держится" ⇒ "хватка есть".
    /// Стопа ставится на кромку со стороны космоса, normal — направление "в корпус".
    /// </summary>
    private bool TryHullGrab(SpiderLegsComponent comp, Vector2 center, Vector2 root, Vector2 want,
        float hullRange, out Vector2 point, out Vector2 normal)
    {
        point = want;
        normal = Vector2.Zero;

        var dir = want - root;
        if (dir.LengthSquared() < 1e-8f)
            dir = want - center;
        if (dir.LengthSquared() < 1e-8f)
            return false;
        dir = Vector2.Normalize(dir);

        var maxDist = hullRange;
        var found = false;
        var bestScore = float.MinValue;

        foreach (var offset in HullProbeAngles)
        {
            var d = new Angle(offset).RotateVec(dir);
            var prev = center;
            var hit = false;

            for (var dist = HullMarchStep; dist <= maxDist; dist += HullMarchStep)
            {
                var q = center + d * dist;
                if (Classify(q) != SpiderCell.Space)
                {
                    hit = true;
                    break;
                }

                prev = q;
            }

            if (!hit)
                continue;

            var reach = (prev - root).Length();
            if (reach > comp.MaxReach || reach < TuckedLength)
                continue;

            var score = reach - MathF.Abs(offset) * AnglePenalty;
            if (score <= bestScore)
                continue;

            bestScore = score;
            point = prev;
            normal = d;
            found = true;
        }

        return found;
    }

    /// <summary>Куда прячется полностью втянутая стопа: чуть наружу от точки крепления.</summary>
    private Vector2 TuckPoint(SpiderLegsComponent comp, Vector2 center, Vector2 root)
    {
        var outward = root - center;
        outward = outward.LengthSquared() > 1e-8f ? Vector2.Normalize(outward) : new Vector2(1f, 0f);
        return root + outward * comp.TuckDistance;
    }

    /// <summary>
    /// Возвращает точку постановки стопы: не в стене и не за стеной.
    /// Точка годится, только если от центра корпуса до неё есть прямая видимость —
    /// иначе стопа могла бы "перешагнуть" тонкую стену в соседнюю комнату.
    /// Если желаемая точка не годится — лучами из центра корпуса ищем границу стены
    /// в нескольких направлениях вокруг желаемого и берём точку, которая дальше всего
    /// от корня ноги и меньше всего отклоняется от желаемого направления.
    /// </summary>
    private Vector2 ResolveFoot(SpiderLegsComponent comp, Vector2 center, Vector2 root, Vector2 want)
    {
        if (_obstacleGridComp == null)
            return want;

        if (HasLineOfSight(center, want))
        {
            var cleared = Clearance(want, comp.WallMargin);
            if (HasLineOfSight(center, cleared))
                return cleared;
        }

        var dir = want - root;
        if (dir.LengthSquared() < 1e-8f)
            dir = want - center;

        var found = false;
        var best = want;
        var bestScore = float.MinValue;

        foreach (var offset in ProbeAngles)
        {
            var candidate = root + new Angle(offset).RotateVec(dir);
            Raycast(center, candidate, out var hit);
            var p = Clearance(hit, comp.WallMargin);

            // Отодвигание от стен не должно уводить точку за угол.
            if (!HasLineOfSight(center, p))
                continue;

            var reach = (p - root).Length();
            if (reach < comp.MinReach)
                continue;

            var score = reach - MathF.Abs(offset) * AnglePenalty;
            if (score <= bestScore)
                continue;

            bestScore = score;
            best = p;
            found = true;
        }

        // Совсем тесно (например, узкий шлюз): прижимаем стопу к стене по прямой.
        if (!found)
        {
            Raycast(center, want, out var hit);
            best = hit;
        }

        return best;
    }

    /// <summary>
    /// Сколько внутренних точек дуги ноги (без корня и стопы) стоят в стене.
    /// Космос не считается: над пустотой нога может висеть как угодно.
    /// </summary>
    private int CountBlocked(Vector2[] joints, int segments)
    {
        if (!_obstaclesActive)
            return 0;

        var count = 0;
        for (var i = 1; i < segments; i++)
        {
            if (Classify(joints[i]) == SpiderCell.Wall)
                count++;
        }

        return count;
    }

    /// <summary>Видна ли точка to из точки from: ни одна клетка на отрезке не заблокирована.</summary>
    private bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        if (!_obstaclesActive)
            return true;

        if (_obstacleGridComp != null)
            return Raycast(from, to, out _);

        // Кресло в космосе: нет одного грида для DDA — идём лучом с мелким шагом.
        var d = to - from;
        var len = d.Length();
        for (var dist = HullMarchStep; dist < len; dist += HullMarchStep)
        {
            if (IsBlocked(from + d * (dist / len)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Луч по клеткам (DDA, Amanatides–Woo): проходит ровно по тем клеткам, которые пересекает отрезок,
    /// поэтому не "проскакивает" тонкие стены. Проход точно через угол двух клеток считается
    /// перекрытым, если заблокирована хоть одна из боковых клеток — щели между стенами по диагонали нет.
    /// </summary>
    /// <returns>true, если путь свободен до конца. hit — конец отрезка или последняя свободная точка перед стеной.</returns>
    private bool Raycast(Vector2 start, Vector2 end, out Vector2 hit)
    {
        hit = start;
        var size = _obstacleGridComp!.TileSize;

        if (IsBlocked(start))
            return false;

        var d = end - start;
        var len = d.Length();
        if (len < 1e-6f)
        {
            hit = end;
            return true;
        }

        // Работаем в единицах клеток.
        var sx = start.X / size;
        var sy = start.Y / size;
        var dx = d.X / size;
        var dy = d.Y / size;

        var tx = (int) MathF.Floor(sx);
        var ty = (int) MathF.Floor(sy);
        var stepX = dx > 0 ? 1 : -1;
        var stepY = dy > 0 ? 1 : -1;

        var tMaxX = MathF.Abs(dx) > 1e-9f ? (dx > 0 ? tx + 1 - sx : sx - tx) / MathF.Abs(dx) : float.MaxValue;
        var tMaxY = MathF.Abs(dy) > 1e-9f ? (dy > 0 ? ty + 1 - sy : sy - ty) / MathF.Abs(dy) : float.MaxValue;
        var tDeltaX = MathF.Abs(dx) > 1e-9f ? 1f / MathF.Abs(dx) : float.MaxValue;
        var tDeltaY = MathF.Abs(dy) > 1e-9f ? 1f / MathF.Abs(dy) : float.MaxValue;

        // Отступ назад от стены вдоль луча (в долях отрезка).
        var back = 0.02f / len;

        // Отрезок короткий, так что клеток немного; ограничитель — на всякий случай.
        for (var guard = 0; guard < 256; guard++)
        {
            var t = MathF.Min(tMaxX, tMaxY);
            if (t > 1f)
            {
                hit = end;
                return true;
            }

            if (MathF.Abs(tMaxX - tMaxY) < 1e-6f)
            {
                // Ровно через угол: перекрыт, если перекрыта любая из двух боковых клеток.
                if (IsTileBlocked(new Vector2i(tx + stepX, ty)) || IsTileBlocked(new Vector2i(tx, ty + stepY)))
                {
                    hit = start + d * MathF.Max(0f, t - back);
                    return false;
                }

                tx += stepX;
                ty += stepY;
                tMaxX += tDeltaX;
                tMaxY += tDeltaY;
            }
            else if (tMaxX < tMaxY)
            {
                tx += stepX;
                tMaxX += tDeltaX;
            }
            else
            {
                ty += stepY;
                tMaxY += tDeltaY;
            }

            if (IsTileBlocked(new Vector2i(tx, ty)))
            {
                hit = start + d * MathF.Max(0f, t - back);
                return false;
            }
        }

        hit = end;
        return true;
    }

    /// <summary>Отодвигает точку от соседних заблокированных клеток на margin (доля клетки).</summary>
    private Vector2 Clearance(Vector2 p, float margin)
    {
        var size = _obstacleGridComp!.TileSize;
        var tx = MathF.Floor(p.X / size);
        var ty = MathF.Floor(p.Y / size);
        var fx = p.X / size - tx;
        var fy = p.Y / size - ty;
        var cx = (tx + 0.5f) * size;
        var cy = (ty + 0.5f) * size;

        var x = p.X;
        var y = p.Y;

        if (fx < margin && IsBlocked(new Vector2(cx - size, cy)))
            x = (tx + margin) * size;
        else if (fx > 1f - margin && IsBlocked(new Vector2(cx + size, cy)))
            x = (tx + 1f - margin) * size;

        if (fy < margin && IsBlocked(new Vector2(cx, cy - size)))
            y = (ty + margin) * size;
        else if (fy > 1f - margin && IsBlocked(new Vector2(cx, cy + size)))
            y = (ty + 1f - margin) * size;

        return new Vector2(x, y);
    }
}
