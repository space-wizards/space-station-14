using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client.IconSmoothing.Smoothings;

/// <summary>
/// This is effectively a 9 slice, but only using 4 instead of 9.
/// Not a 4 slice, that's apparently a name for toasters...
/// </summary>
[Virtual]
public partial class CornerSpriteSmoothing : ISpriteSmoothState
{
    [DataField(required:true)]
    public string Base { get; set; }

    [DataField(required:true)]
    public HashSet<string> Mask { get; set; }

    [DataField]
    public string LayerKey { get; set; } = "corner";

    [DataField]
    public int? Index { get; set; }

    [DataField]
    public ProtoId<ShaderPrototype>? Shader { get; set; }

    public virtual void InitializeStates(Entity<SpriteComponent> entity, SpriteSystem sprite)
    {
        foreach (var offset in Enum.GetValues<DirectionOffset>())
        {
            var key = GetLayerKey(offset);
            sprite.LayerMapSet(entity.AsNullable(), key, sprite.AddRsiLayer(entity.AsNullable(), GetState(0, 0), index: Index));
            sprite.LayerSetDirOffset(entity.AsNullable(), key, offset);

            if (Shader != null)
                entity.Comp.LayerSetShader(key, Shader);
        }
    }

    public virtual IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers)
    {
        var match = Direction8Flag.None;
        byte seen = 0;
        for (byte i = 0; i < IconSmoothSystem.Directions; i++)
        {
            if (layers[i] is { } keys && keys.Overlaps(Mask))
                match |= (Direction8Flag)(1 << i);

            if (!GetOrthoganals(i, out var mask))
                continue;

            yield return (GetLayerKey(i), GetState((byte)(match & mask), seen));
            seen += 2;
        }
    }

    protected string GetLayerKey(DirectionOffset i)
    {
        var direction = i switch
        {
            DirectionOffset.None => Direction.SouthEast,
            DirectionOffset.Clockwise => Direction.SouthWest,
            DirectionOffset.CounterClockwise => Direction.NorthEast,
            DirectionOffset.Flip => Direction.NorthWest,
            _ => throw new ArgumentOutOfRangeException(nameof(Direction), i, null)
        };

        return LayerKey + direction;
    }

    protected string GetLayerKey(byte i)
    {
        var direction = i switch
        {
            // John Shitcode called he said it's joever.
            2 => Direction.SouthEast,
            4 => Direction.NorthEast,
            6 => Direction.NorthWest,
            7 => Direction.SouthWest,
            _ => throw new ArgumentOutOfRangeException(nameof(Direction), i, null)
        };

        return LayerKey + direction;
    }

    private string GetState(byte directions, byte offset)
    {
        return Base + GetAppendix(directions, offset);
    }

    protected virtual byte GetAppendix(byte directions, byte offset)
    {
        // Need to shift these to the right so we only get the 3 relevant directions:tm:
        // Amazed that C# doesn't have a circular bitshift built in for bytes. Kinda fucked up.
        var appendix = offset > 0 ? (byte)((directions >> offset) | (directions << (8 - offset))) : directions;
        DebugTools.Assert(appendix < 8, $"Calculated appendix of {appendix} went above the maximum of 8, directions: {directions}, offset {offset}");
        return appendix;
    }

    protected bool GetOrthoganals(byte i, out Direction8Flag directions)
    {
        // The stupid way!!!
        switch (i)
        {
            case 2:
                directions = Direction8Flag.South | Direction8Flag.SouthEast | Direction8Flag.East;
                return true;
            case 4:
                directions = Direction8Flag.East | Direction8Flag.NorthEast | Direction8Flag.North;
                return true;
            case 6:
                directions = Direction8Flag.North | Direction8Flag.NorthWest | Direction8Flag.West;
                return true;
            case 7:
                directions = Direction8Flag.West | Direction8Flag.SouthWest | Direction8Flag.South;
                return true;
            default:
                directions = Direction8Flag.None;
                return false;
        }
    }
}
