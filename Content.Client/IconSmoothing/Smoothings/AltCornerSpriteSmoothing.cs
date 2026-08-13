namespace Content.Client.IconSmoothing.Smoothings;

/// <summary>
/// An expansion of <see cref="CornerSpriteSmoothing"/> that allows for an alternative state with an alternative lookup key!
/// TODO: THIS SHIT DON'T WORK AND IS A WORSE VERSION OF 9s!!! KILL THIS AND REPLACE IT WITH 9 SPRITE SMOOTHING!!!
/// </summary>
public sealed partial class AltCornerSpriteSmoothing : CornerSpriteSmoothing
{
    /// <summary>
    /// Alternative Base for when we match on our <see cref="AltMask"/>
    /// </summary>
    [DataField(required:true)]
    public string AltBase { get; set; }

    /// <summary>
    /// An Additional mask of keys we compare against, which can provide additional data.
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

            yield return (GetLayerKey(i), GetState(match & mask, altmatch & mask, seen));
            seen += 2;
        }
    }

    private string GetState(Direction8Flag directions, Direction8Flag altDirections, byte offset)
    {
        // No new data to be gained from alt directions.
        if ((directions & altDirections) == altDirections)
            return Base + GetAppendix((byte)directions, offset);

        return GetAltState((byte)(directions | altDirections), offset);
    }

    private string GetAltState(byte directions, byte offset)
    {
        return AltBase + GetAppendix(directions, offset);
    }
}
