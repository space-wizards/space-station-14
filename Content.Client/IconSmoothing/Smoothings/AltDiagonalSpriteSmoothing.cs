using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.IconSmoothing.Smoothings;

/// <summary>
/// Calculates a sprite state from Cardinal Direction flags.
/// Shrimple as that.
/// </summary>
public sealed partial class AltDiagonalSpriteSmoothing : CornerSpriteSmoothing
{
    /// <summary>
    /// An Alternative Mask of keys which we compare against if <see cref="ISpriteSmoothState.Mask"/> fails
    /// </summary>
    [DataField(required:true)]
    public HashSet<string> AltMask { get; set; }

    public override void InitializeStates(Entity<SpriteComponent> entity, SpriteSystem sprite)
    {
        sprite.LayerMapSet(entity.AsNullable(), LayerKey, sprite.AddRsiLayer(entity.AsNullable(), Base + 0, index: Index));

        if (Shader != null)
            entity.Comp.LayerSetShader(LayerKey, Shader);
    }

    public override IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers)
    {
        if (!GetCorners(2, out var mask))
        {
            DebugTools.Assert($"Hardcoded check in {nameof(DiagonalSpriteSmoothing)} failed to return a mask!");
            yield break;
        }

        var match = Direction8Flag.None;
        var altmatch = Direction8Flag.None;
        for (byte i = 0; i <= (byte)Direction.East; i++)
        {
            if (layers[i] is not { } keys)
                continue;

            if (keys.Overlaps(Mask))
                match |= (Direction8Flag)(1 << i);

            if (keys.Overlaps(AltMask))
                altmatch |= (Direction8Flag)(1 << i);
        }

        yield return (LayerKey, GetState((match & mask) == mask, (altmatch & mask) == mask));
    }

    private string GetState(bool match, bool altMatch)
    {
        if (match)
            return Base + 1;

        if (altMatch)
            return Base + 2;

        return Base + 0;
    }
}
