using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.IconSmoothing.Smoothings;

public sealed partial class EdgeSpriteSmoothing : ISpriteSmoothState
{
    [DataField(required:true)]
    public string Base { get; set; }

    [DataField(required:true)]
    public HashSet<string> Mask { get; set; }

    [DataField]
    public string LayerKey { get; set; } = "edge";

    [DataField]
    public int? Index { get; set; }

    [DataField]
    public ProtoId<ShaderPrototype>? Shader { get; set; }

    public void InitializeStates(Entity<SpriteComponent> entity, SpriteSystem sprite)
    {
        for (byte i = 0; i < 8; i+= 2)
        {
            var direction = (Direction)i;
            var key = LayerKey + GetSuffix(i);
            sprite.LayerMapSet(entity.AsNullable(), key, sprite.AddRsiLayer(entity.AsNullable(), Base + GetSuffix(i), index: Index));
            sprite.LayerSetOffset(entity.AsNullable(), key, direction.ToIntVec());
            sprite.LayerSetVisible(entity.AsNullable(), key, false);
        }
    }

    public IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers, Entity<SpriteComponent> entity, SpriteSystem sprite)
    {
        for (byte i = 0; i < 8; i+= 2)
        {
            // We actually don't want to smooth if it overlaps!
            if (layers[i] is { } keys && keys.Overlaps(Mask))
                sprite.LayerSetVisible(entity.AsNullable(), LayerKey + GetSuffix(i), false);
            else
                sprite.LayerSetVisible(entity.AsNullable(), LayerKey + GetSuffix(i), true);
        }

        yield break;
    }

    public string GetSuffix(byte i)
    {
        return i switch
        {
            0 => "south",
            2 => "east",
            4 => "north",
            6 => "west",
            _ => throw new ArgumentOutOfRangeException(nameof(i), i, null)
        };
    }
}
