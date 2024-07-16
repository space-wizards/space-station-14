using Content.Shared.DisplacementMap;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager;

namespace Content.Client.DisplacementMap;

public sealed class DisplacementMapSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public bool TryAddDisplacement(DisplacementData data, SpriteComponent sprite, int index, string key, HashSet<string> revealedLayers)
    {
        if (data.ShaderOverride != null)
            sprite.LayerSetShader(index, data.ShaderOverride);

        //allows you not to write it every time in the YML
        if (data.Layer.CopyToShaderParameters == null)
        {
            data.Layer.CopyToShaderParameters = new()
            {
                LayerKey = "dummy",
                ParameterTexture = "displacementMap",
                ParameterUV = "displacementUV",
            };
        }

        var displacementKey = $"{key}-displacement";
        if (!revealedLayers.Add(displacementKey))
        {
            Log.Warning($"Duplicate key for DISPLACEMENT: {displacementKey}.");
            return false;
        }

        var displacementLayer = _serialization.CreateCopy(data.Layer, notNullableOverride: true);
        displacementLayer.CopyToShaderParameters!.LayerKey = key;

        sprite.AddLayer(displacementLayer, index);
        sprite.LayerMapSet(displacementKey, index);

        revealedLayers.Add(displacementKey);

        return true;
    }
}
