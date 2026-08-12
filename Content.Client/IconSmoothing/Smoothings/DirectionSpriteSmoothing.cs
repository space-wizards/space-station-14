using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.IconSmoothing.Smoothings;

/// <summary>
/// Calculates a sprite state from Cardinal Direction flags.
/// Shrimple as that.
/// </summary>
public sealed partial class DirectionSpriteSmoothing : ISpriteSmoothState
{
    [DataField(required:true)]
    public string Base { get; set; }

    [DataField(required:true)]
    public HashSet<string> Mask { get; set; }

    [DataField]
    public string LayerKey { get; set; } = "direction";

    [DataField]
    public int? Index { get; set; }

    [DataField]
    public ProtoId<ShaderPrototype>? Shader { get; set; }

    public void InitializeStates(Entity<SpriteComponent> entity, SpriteSystem sprite)
    {
        sprite.LayerMapSet(entity.AsNullable(), LayerKey, sprite.AddRsiLayer(entity.AsNullable(), Base + 0, index: Index));

        if (Shader != null)
            entity.Comp.LayerSetShader(LayerKey, Shader);
    }

    public IEnumerable<(string key, string state)> EnumerateStates(HashSet<string>?[] layers)
    {
        var match = DirectionFlag.None;
        for (byte i = 0; 2 * i < IconSmoothSystem.Directions; i++)
        {
            if (layers[2 * i] is { } keys && keys.Overlaps(Mask))
                match |= (DirectionFlag)(1 << i);
        }

        yield return (LayerKey, Base + (byte)match);
    }
}
