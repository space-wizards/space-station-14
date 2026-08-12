namespace Content.Client.IconSmoothing.Smoothings;

/// <summary>
/// An expansion of <see cref="CornerSpriteSmoothing"/> that allows for an alternative state with an alternative lookup key!
/// </summary>
public sealed partial class AltCornerSpriteSmoothing : CornerSpriteSmoothing
{
    /// <summary>
    /// Alternative Base for when we match on our <see cref="AltMask"/>
    /// </summary>
    [DataField(required:true)]
    public string AltBase { get; set; }

    /// <summary>
    /// An Alternative Mask of keys which we compare against if <see cref="ISpriteSmoothState.Mask"/> fails
    /// </summary>
    [DataField(required:true)]
    public HashSet<string> AltMask { get; set; }

    public override IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers)
    {
        var match = Direction8Flag.None;
        var altmatch = Direction8Flag.None;
        byte seen = 0;
        for (byte i = 0; i < IconSmoothSystem.Directions; i++)
        {
            if (layers[i] is { } keys)
            {
                if (keys.Overlaps(Mask))
                    match |= (Direction8Flag)(1 << i);

                if (keys.Overlaps(AltMask))
                    altmatch |= (Direction8Flag)(1 << i);
            }

            if (!GetOrthoganals(i, out var mask))
                continue;

            yield return (GetLayerKey(i), GetState((byte)(match & mask), (byte)(altmatch & mask), seen));
            seen += 2;
        }
    }

    private string GetState(byte directions, byte altDirections, byte offset)
    {
        if (directions == 0 && altDirections > 0)
            return GetAltState(altDirections, offset);

        return Base + GetAppendix(directions, offset);
    }

    private string GetAltState(byte directions, byte offset)
    {
        return AltBase + GetAppendix(directions, offset);
    }
}
