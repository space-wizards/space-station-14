using System.Numerics;

namespace Content.Shared.NPC;

public abstract partial class SharedPathfindingSystem : EntitySystem
{
    /// <summary>
    /// This is equivalent to agent radii for navmeshes. In our case it's preferable that things are cleanly
    /// divisible per tile so we'll make sure it works as a discrete number.
    /// </summary>
    public const byte SubStep = 4;

    public const byte ChunkSize = 8;
    public static readonly Vector2 ChunkSizeVec = new(ChunkSize, ChunkSize);

    /// <summary>
    /// We won't do points on edges so we'll offset them slightly.
    /// </summary>
    protected const float StepOffset = 1f / SubStep / 2f;

    private static readonly Vector2 StepOffsetVec = new(StepOffset, StepOffset);

    public Vector2 GetCoordinate(Vector2i chunk, Vector2i index)
    {
        return new Vector2(index.X, index.Y) / SubStep+ (chunk) * ChunkSizeVec + StepOffsetVec;
    }

    public static float ManhattanDistance(Vector2i start, Vector2i end)
    {
        var distance = end - start;
        return Math.Abs(distance.X) + Math.Abs(distance.Y);
    }

    public static float OctileDistance(Vector2i start, Vector2i end)
    {
        var diff = start - end;
        var ab = Vector2.Abs(diff);
        return ab.X + ab.Y + (1.41f - 2) * Math.Min(ab.X, ab.Y);
    }

    public static IEnumerable<Vector2i> GetTileOutline(Vector2i center, float radius)
    {
        // https://www.redblobgames.com/grids/circle-drawing/
        var vecCircle = center + Vector2.One / 2f;

        for (var r = 0; r <= Math.Floor(radius * MathF.Sqrt(0.5f)); r++)
        {
            var d = MathF.Floor(MathF.Sqrt(radius * radius - r * r));

            yield return new Vector2(vecCircle.X - d, vecCircle.Y + r).Floored();

            yield return new Vector2(vecCircle.X + d, vecCircle.Y + r).Floored();

            yield return new Vector2(vecCircle.X - d, vecCircle.Y - r).Floored();

            yield return new Vector2(vecCircle.X + d, vecCircle.Y - r).Floored();

            yield return new Vector2(vecCircle.X + r, vecCircle.Y - d).Floored();

            yield return new Vector2(vecCircle.X + r, vecCircle.Y + d).Floored();

            yield return new Vector2(vecCircle.X - r, vecCircle.Y - d).Floored();

            yield return new Vector2(vecCircle.X - r, vecCircle.Y + d).Floored();
        }
    }
}
