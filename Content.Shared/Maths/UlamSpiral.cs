namespace Content.Shared.Maths;

public static class UlamSpiral
{
    /// <summary>
    ///     Algorithm for mapping scalars to 2D positions in the same pattern as an Ulam Spiral.
    /// </summary>
    /// <param name="n">Scalar to map to a 2D position. Returns a zero vector for values smaller than 1.</param>
    /// <returns>The mapped 2D position for the scalar.</returns>
    public static Vector2i Point(int n)
    {
        if (n <= 0)
            return new Vector2i(0, 0);

        var k = (int)MathF.Ceiling((MathF.Sqrt(n) - 1) / 2);
        var t = 2 * k + 1;
        var m = (int)MathF.Pow(t, 2);
        t--;

        if (n >= m - t)
            return new Vector2i(k - (m - n), -k);

        m -= t;

        if (n >= m - t)
            return new Vector2i(-k, -k + (m - n));

        m -= t;

        if (n >= m - t)
            return new Vector2i(-k + (m - n), k);

        return new Vector2i(k, k - (m - n - t));
    }

    /// <summary>
    ///     Returns the largest value for which <see cref="Point"> will generate a point within a <paramref name="maxDistance"> Chebyshev distance from origin.
    /// </summary>
    public static int PointsForMaxDistance(int maxDistance)
    {
        var x = maxDistance * 2 + 1;
        return x * x;
    }
}
