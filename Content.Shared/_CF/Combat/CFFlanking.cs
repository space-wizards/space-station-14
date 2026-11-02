using System.Numerics;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Pathfinder 2e flanking on free movement.
/// Two allies flank when each is within reach and the line between them passes through the foe.
/// </summary>
public static class CFFlanking
{
    /// <summary>Off-guard is a −2 circumstance penalty to AC.</summary>
    public const int OffGuardAcPenalty = 2;

    /// <summary>Normal melee reach. The reach trait is 10.</summary>
    public const int DefaultReachFeet = 5;

    /// <summary>Largest lookup radius, in world units. Normal queries are much smaller than this.</summary>
    public const float SearchRange = 8f;

    /// <summary>
    /// The line between the allies must pass within this distance of the foe's position.
    /// About a quarter of a tile, so it has to cross the creature rather than merely pass nearby.
    /// </summary>
    public const float BodyRadius = 0.25f;

    public static int ReachInTiles(int reachFeet)
    {
        if (reachFeet <= 0)
            return 0;

        return Math.Max(1, reachFeet / (int)CFMelee.FeetPerTile);
    }

    /// <summary>
    /// World-unit reach. Five feet matches a normal melee swing, so standing on the foe's tile still counts.
    /// </summary>
    public static float ReachInUnits(int reachFeet)
    {
        if (reachFeet <= 0)
            return 0f;

        return reachFeet / (float)DefaultReachFeet * CFMelee.MeleeRange;
    }

    public static bool InReach(Vector2 from, Vector2 target, int reachFeet)
    {
        var reach = ReachInUnits(reachFeet);
        if (reach <= 0f)
            return false;

        var delta = from - target;
        return delta.LengthSquared() <= reach * reach;
    }

    /// <summary>
    /// SS14 melee <c>Range</c> that reaches the far corner of the reach square
    /// without also reaching the next orthogonal square.
    /// </summary>
    public static float Ss14SwingRange(int reachFeet)
    {
        return ReachInTiles(reachFeet) * MathF.Sqrt(2f) + 0.05f;
    }

    /// <summary>
    /// True when the segment from <paramref name="a"/> to <paramref name="b"/> crosses the foe.
    /// The foe has to lie between the two allies, not off to one side.
    /// </summary>
    public static bool LineFlanks(Vector2 a, Vector2 b, Vector2 target)
    {
        var ab = b - a;
        var lengthSq = ab.LengthSquared();
        if (lengthSq < 0.0001f)
            return false;

        var t = Vector2.Dot(target - a, ab) / lengthSq;
        if (t <= 0.02f || t >= 0.98f)
            return false;

        var closest = a + ab * t;
        var miss = target - closest;
        return miss.LengthSquared() <= BodyRadius * BodyRadius;
    }
}
