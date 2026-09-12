using Robust.Client.GameObjects;

namespace Content.Client.IconSmoothing.Smoothings;

/// <summary>
/// An alternative of <see cref="CornerSpriteSmoothing"/> which is based off of 9s with more states dedicated to corners:tm:
/// </summary>
public sealed partial class CardinalCornerSpriteSmoothing : CornerSpriteSmoothing
{
    /// <summary>
    /// An Additional mask of keys we compare against, which can provide additional data.
    /// </summary>
    [DataField(required:true)]
    public HashSet<string> AltMask { get; set; }

    public override IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers, Entity<SpriteComponent> entity, SpriteSystem sprite)
    {
        var match = Direction8Flag.None;
        var altMatch = Direction8Flag.None;
        byte seen = 0;
        for (byte i = 0; i < IconSmoothSystem.Directions; i++)
        {
            var mask = (Direction8Flag)(1 << i);
            if (layers[i] is { } keys)
            {
                if (keys.Overlaps(Mask))
                    match |= mask;

                if (keys.Overlaps(AltMask))
                    altMatch |= mask;
            }

            if (!GetCorners(i, out mask))
                continue;

            yield return (GetCornerLayerKey(i), GetCornerState((byte)(match & mask), (byte)(altMatch & mask), seen));
            seen += 2;
        }
    }

    private byte ConvertToCardinal(byte flag)
    {
        // Shift bits down, abusing that we only have 8 bits.
        flag &= 0x55;
        flag |= (byte)(flag >> 1);
        flag &= 0x33;
        flag |= (byte)(flag >> 2);
        flag &= 0xF; // Remove all trailing bits, only keep the final 4

        return flag;
    }

    /// <remarks>
    /// This is very hardcoded and gross, could use a better mathematical solution but codewise it's fine.
    /// </remarks>
    private string GetCornerState(byte directions, byte altDirections, byte offset)
    {
        directions = Offset(directions, offset);
        altDirections = Offset(altDirections, offset);

        return GetCornerState(directions, altDirections);
    }

    /// <remarks>
    /// This is very hardcoded and gross, could use a better mathematical solution but codewise it's fine.
    /// </remarks>
    private string GetCornerState(byte directions, byte altDirections)
    {
        // No corner data, so alt and normal directions use the same data.
        var cardinals = ConvertToCardinal(directions);
        var cardinalMask = (byte)(cardinals | ConvertToCardinal(altDirections));

        // If we don't have corners to render, then don't render them.
        if (cardinalMask < 3)
        {
            if (cardinalMask != cardinals)
                return $"{Base}{cardinalMask + 2}";

            return $"{Base}{cardinals}";
        }

        // If there's no relevant alt data, do a simple conversion.
        if (cardinalMask == cardinals)
        {
            if ((directions & 2) == 2)
                return $"{Base}6";

            if ((altDirections & 2) == 2)
                return $"{Base}7";

            return $"{Base}5";
        }

        if ((directions & 2) == 2 || (altDirections & 2) == 2)
            return $"{Base}{cardinals + 11}";

        return $"{Base}{cardinals + 8}";
    }
}
