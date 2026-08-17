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
            var key = LayerKey + direction;
            sprite.LayerMapSet(entity.AsNullable(), key, sprite.AddRsiLayer(entity.AsNullable(), Base + direction, index: Index));
            sprite.LayerSetOffset(entity.AsNullable(), key, direction.ToIntVec());
            sprite.LayerSetVisible(entity.AsNullable(), key, false);
        }
    }

    public IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers, Entity<SpriteComponent> entity, SpriteSystem sprite)
    {
        for (byte i = 0; i < 8; i+= 2)
        {
            var direction = (Direction)i;

            // We actually don't want to smooth if it overlaps!
            if (layers[i] is { } keys && keys.Overlaps(Mask))
                sprite.LayerSetVisible(entity.AsNullable(), LayerKey + direction, false);
            else
                sprite.LayerSetVisible(entity.AsNullable(), LayerKey + direction, true);
        }

        yield break;
    }
}
