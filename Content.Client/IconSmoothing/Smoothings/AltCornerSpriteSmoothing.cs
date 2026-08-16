using Robust.Client.GameObjects;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.IconSmoothing.Smoothings;

/// <summary>
/// An alternative of <see cref="CornerSpriteSmoothing"/> which is based off of 9s with more states dedicated to corners:tm:
/// </summary>
public sealed partial class AltCornerSpriteSmoothing : CornerSpriteSmoothing
{
    /// <summary>
    /// Base string for cardinal based states of which 3 files should exist.
    /// </summary>
    [DataField(required: true)]
    public string CardinalBase;

    /// <summary>
    /// An Additional mask of keys we compare against, which can provide additional data.
    /// </summary>
    [DataField(required:true)]
    public HashSet<string> AltMask { get; set; }

    protected override void InitializeOffset(Entity<SpriteComponent> entity, SpriteSystem sprite, DirectionOffset offset)
    {
        base.InitializeOffset(entity, sprite, offset);
        var key = GetCardinalLayerKey((byte)(2 * (byte)offset));
        sprite.LayerMapSet(entity.AsNullable(), key, sprite.AddRsiLayer(entity.AsNullable(), CardinalBase + 0, index: Index));
        sprite.LayerSetDirOffset(entity.AsNullable(), key, offset);

        if (Shader != null)
            entity.Comp.LayerSetShader(key, Shader);
    }

    public override IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers)
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

            // If even, then we're on a cardinal and have a state!
            if (i % 2 == 0)
                yield return (GetCardinalLayerKey(i), GetCardinalState(match, altMatch, mask));

            if (!GetCorners(i, out mask))
                continue;

            yield return (GetCornerLayerKey(i), GetCornerState((byte)(match & mask), (byte)(altMatch & mask), seen));
            seen += 2;
        }
    }

    private DirectionFlag ConvertToCardinal(byte flag)
    {
        // Shift bits down, abusing that we only have 8 bits.
        flag &= 0x55;
        flag |= (byte)(flag >> 1);
        flag &= 0x33;
        flag |= (byte)(flag >> 2);

        // Trailing 2 bits are dropped in this conversion so we don't have to remove them above.
        return (DirectionFlag)flag;
    }

    private string GetCardinalLayerKey(byte i)
    {
        return LayerKey + (Direction)i;
    }

    private string GetCardinalState(Direction8Flag directions, Direction8Flag altDirections, Direction8Flag mask)
    {
        if ((mask & directions) > 0)
            return CardinalBase + 1;

        if ((mask & altDirections) > 0)
            return CardinalBase + 2;

        return CardinalBase + 0;
    }

    /// <remarks>
    /// This is very hardcoded and gross, could use a better mathematical solution but codewise it's fine.
    /// </remarks>
    private string GetCornerState(byte directions, byte altDirections, byte offset)
    {
        directions = Offset(directions, offset);
        altDirections = Offset(altDirections, offset);

        return GetCornerState(
            ConvertToCardinal(directions),
            ConvertToCardinal(altDirections),
            (directions & 2) == 2 || (altDirections & 2) == 2); // 2 == Direction8Flag.SouthEast, so we check if either have a corner it can smooth with!
    }

    /// <remarks>
    /// This is very hardcoded and gross, could use a better mathematical solution but codewise it's fine.
    /// </remarks>
    private string GetCornerState(DirectionFlag directions, DirectionFlag altDirections, bool corner)
    {
        // No corner data, so alt and normal directions use the same data.
        byte mask = (byte)(directions | altDirections);
        if (!corner || mask < 3)
            return Base + mask;

        return Base + (4 + (byte)altDirections);
    }
}
