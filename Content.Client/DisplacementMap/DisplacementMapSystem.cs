using Content.Shared.DisplacementMap;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.Manager;

namespace Content.Client.DisplacementMap;

public sealed class DisplacementMapSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = default!;

    /// <summary>
    /// Attempting to apply a displacement map to a specific layer of SpriteComponent
    /// </summary>
    /// <param name="data">Information package for applying the displacement map</param>
    /// <param name="sprite">SpriteComponent</param>
    /// <param name="index">Index of the layer where the new map layer will be added</param>
    /// <param name="key">Unique layer key, which will determine which layer to apply displacement map to</param>
    /// <param name="displacementKey">The key of the new displacement map layer added by this function.</param>
    /// <returns></returns>
    public bool TryAddDisplacement(DisplacementData data,
        SpriteComponent sprite,
        int index,
        object key,
        out string displacementKey)
    {
        displacementKey = $"{key}-displacement";

        if (key.ToString() is null)
            return false;

        if (data.ShaderOverride != null)
            sprite.LayerSetShader(index, data.ShaderOverride);

        if (sprite.LayerMapTryGet(displacementKey, out var oldIndex))
            sprite.RemoveLayer(oldIndex);

        //allows you not to write it every time in the YML
        foreach (var pair in data.SizeMaps)
        {
            pair.Value.CopyToShaderParameters ??= new()
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
            {
                Log.Warning(
                    $"DISPLACEMENT: {displacementKey} has a resolution that is not 1:1, things can look crooked");
            }

            var layerSize = actualRSI.Size.X;
            if (data.SizeMaps.TryGetValue(layerSize, out var map))
                displacementDataLayer = map;
        }

        var displacementLayer = _serialization.CreateCopy(displacementDataLayer, notNullableOverride: true);
        displacementLayer.CopyToShaderParameters!.LayerKey = key.ToString() ?? "this is impossible";

        sprite.AddLayer(displacementLayer, index);
        sprite.LayerMapSet(displacementKey, index);

        return true;
    }
}
