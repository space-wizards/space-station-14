using System.Numerics;

namespace Content.Shared.CF.Spells;

/// <summary>
/// Pathfinder 2e remaster areas on the map grid. One tile is 5 feet.
/// This is the real tile size, not the 1.25 melee reach used for Strikes.
/// </summary>
public static class CFSpellGrid
{
    public const float FeetPerTile = 5f;

    public static Vector2i ToTile(Vector2 local, float tileSize)
    {
        var size = tileSize <= 0f ? 1f : tileSize;
        return new Vector2i((int) MathF.Floor(local.X / size), (int) MathF.Floor(local.Y / size));
    }

    public static HashSet<Vector2i> Collect(
        CFSpellAreaKind area,
        int areaFeet,
        Vector2 casterLocal,
        Vector2 aimLocal,
        float tileSize,
        bool includeSelf)
    {
        var size = tileSize <= 0f ? 1f : tileSize;
        var tiles = new HashSet<Vector2i>();
        var radius = Math.Max(1, areaFeet / 5);
        var casterTile = ToTile(casterLocal, size);

        switch (area)
        {
            case CFSpellAreaKind.Burst:
                CollectBurst(tiles, aimLocal, size, radius);
                break;
            case CFSpellAreaKind.Cone:
                CollectCone(tiles, casterTile, casterLocal, aimLocal, size, radius);
                break;
            case CFSpellAreaKind.Emanation:
                CollectEmanation(tiles, casterTile, radius, includeSelf);
                break;
            case CFSpellAreaKind.Line:
                CollectLine(tiles, casterTile, aimLocal - casterLocal, radius);
                break;
            case CFSpellAreaKind.Square:
                CollectSquare(tiles, aimLocal, size, radius);
                break;
        }

        return tiles;
    }

    /// <summary>
    /// Burst from the nearest grid corner. A square counts when any part of it is within the radius.
    /// </summary>
    private static void CollectBurst(HashSet<Vector2i> tiles, Vector2 aimLocal, float tileSize, int radius)
    {
        var scaled = aimLocal / tileSize;
        var corner = new Vector2i(
            (int) MathF.Round(scaled.X, MidpointRounding.AwayFromZero),
            (int) MathF.Round(scaled.Y, MidpointRounding.AwayFromZero));

        for (var x = corner.X - radius - 1; x <= corner.X + radius; x++)
        {
            for (var y = corner.Y - radius - 1; y <= corner.Y + radius; y++)
            {
                if (DistanceToSquare(corner.X, corner.Y, x, y) <= radius)
                    tiles.Add(new Vector2i(x, y));
            }
        }
    }

    /// <summary>
    /// Quarter-circle from an edge (orthogonal) or corner (diagonal) of the caster's space.
    /// The cone does not overlap the caster's square. Depth is the radius in squares.
    /// </summary>
    private static void CollectCone(
        HashSet<Vector2i> tiles,
        Vector2i casterTile,
        Vector2 casterLocal,
        Vector2 aimLocal,
        float tileSize,
        int radius)
    {
        var aim = aimLocal - casterLocal;
        var dir = Snap8(aim);
        var step = Snap8Step(dir);
        var orthogonal = step.X == 0 || step.Y == 0;
        var origin = orthogonal
            ? new Vector2(casterTile.X + 0.5f + step.X * 0.5f, casterTile.Y + 0.5f + step.Y * 0.5f)
            : new Vector2(step.X > 0 ? casterTile.X + 1f : casterTile.X, step.Y > 0 ? casterTile.Y + 1f : casterTile.Y);

        var limit = MathF.Cos(MathF.PI / 4f) - 0.02f;
        for (var x = casterTile.X - radius - 1; x <= casterTile.X + radius + 1; x++)
        {
            for (var y = casterTile.Y - radius - 1; y <= casterTile.Y + radius + 1; y++)
            {
                var tile = new Vector2i(x, y);
                if (tile == casterTile)
                    continue;

                var center = new Vector2(x + 0.5f, y + 0.5f);
                var delta = center - origin;
                var distance = delta.Length();
                if (distance > radius + 0.01f || distance < 0.001f)
                    continue;

                if (Vector2.Dot(delta / distance, dir) < limit)
                    continue;

                tiles.Add(tile);
            }
        }

        // The first square shares an edge, or a corner when the aim is diagonal.
        if (step != Vector2i.Zero)
            tiles.Add(casterTile + step);
    }

    private static void CollectEmanation(HashSet<Vector2i> tiles, Vector2i casterTile, int radius, bool includeSelf)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (Math.Max(Math.Abs(x), Math.Abs(y)) > radius)
                    continue;

                if (x == 0 && y == 0 && !includeSelf)
                    continue;

                tiles.Add(casterTile + new Vector2i(x, y));
            }
        }
    }

    /// <summary>A 5-foot-wide line. It starts on the next square and runs straight for the length.</summary>
    private static void CollectLine(HashSet<Vector2i> tiles, Vector2i casterTile, Vector2 aim, int length)
    {
        var step = Snap8Step(Snap8(aim));
        if (step == Vector2i.Zero)
            step = new Vector2i(1, 0);

        var tile = casterTile;
        for (var i = 0; i < length; i++)
        {
            tile += step;
            tiles.Add(tile);
        }
    }

    private static void CollectSquare(HashSet<Vector2i> tiles, Vector2 aimLocal, float tileSize, int size)
    {
        var origin = ToTile(aimLocal, tileSize);
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
                tiles.Add(origin + new Vector2i(x, y));
        }
    }

    private static float DistanceToSquare(int cornerX, int cornerY, int tileX, int tileY)
    {
        var dx = 0f;
        if (cornerX < tileX)
            dx = tileX - cornerX;
        else if (cornerX > tileX + 1)
            dx = cornerX - (tileX + 1);

        var dy = 0f;
        if (cornerY < tileY)
            dy = tileY - cornerY;
        else if (cornerY > tileY + 1)
            dy = cornerY - (tileY + 1);

        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static Vector2 Snap8(Vector2 aim)
    {
        if (aim.LengthSquared() < 0.0001f)
            return new Vector2(1f, 0f);

        var snapped = MathF.Round(MathF.Atan2(aim.Y, aim.X) / (MathF.PI / 4f)) * (MathF.PI / 4f);
        return new Vector2(MathF.Cos(snapped), MathF.Sin(snapped));
    }

    private static Vector2i Snap8Step(Vector2 dir)
    {
        return new Vector2i(
            MathF.Abs(dir.X) < 0.2f ? 0 : dir.X > 0f ? 1 : -1,
            MathF.Abs(dir.Y) < 0.2f ? 0 : dir.Y > 0f ? 1 : -1);
    }
}
