using Content.Shared.DisplacementMap;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.Manager;

namespace Content.Client.DisplacementMap;

public sealed class DisplacementMapSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public bool TryAddDisplacement(DisplacementData data, SpriteComponent sprite, int index, string key, HashSet<string> revealedLayers)
    {
        if (data.ShaderOverride != null)
            sprite.LayerSetShader(index, data.ShaderOverride);

        var displacementKey = $"{key}-displacement";
        if (!revealedLayers.Add(displacementKey))
        {
            Log.Warning($"Duplicate key for DISPLACEMENT: {displacementKey}.");
            return false;
        }

        //allows you not to write it every time in the YML
        foreach (var pair in data.SizeMaps)
        {
            pair.Value.CopyToShaderParameters??= new()
            {
                LayerKey = "dummy",
                ParameterTexture = "displacementMap",
                ParameterUV = "displacementUV",
            };
        }

        if (!data.SizeMaps.ContainsKey(32))
        {
            Log.Error($"DISPLACEMENT: {displacementKey} don't have 32x32 default displacement map");
            return false;
        }

        // We choose a displacement map from the possible ones, matching the size with the original layer size.
        // If there is no such a map, we use a standard 32 by 32 one
        var displacementDataLayer = data.SizeMaps[EyeManager.PixelsPerMeter];
        var actualRSI = sprite.LayerGetActualRSI(index);
        if (actualRSI is not null)
        {
            if (actualRSI.Size.X != actualRSI.Size.Y)
                Log.Warning($"DISPLACEMENT: {displacementKey} has a resolution that is not 1:1, things can look crooked");

            var layerSize = actualRSI.Size.X;
            if (data.SizeMaps.ContainsKey(layerSize))
                displacementDataLayer = data.SizeMaps[layerSize];
        }

        var displacementLayer = _serialization.CreateCopy(displacementDataLayer, notNullableOverride: true);
        displacementLayer.CopyToShaderParameters!.LayerKey = key;

        sprite.AddLayer(displacementLayer, index);
        sprite.LayerMapSet(displacementKey, index);

        revealedLayers.Add(displacementKey);

        return true;
    }
}
