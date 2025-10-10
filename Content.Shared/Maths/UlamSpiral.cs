namespace Content.Shared.Maths;

public static class UlamSpiral
{
    /// <summary>
    ///     Algorithm for mapping scalars to 2D positions in the same pattern as an Ulam Spiral.
    /// </summary>
    /// <param name="n">Scalar to map to a 2D position. Must be greater than or equal to 1.</param>
    /// <returns>The mapped 2D position for the scalar.</returns>
    private Vector2i UlamSpiral(int n)
    {
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
}
