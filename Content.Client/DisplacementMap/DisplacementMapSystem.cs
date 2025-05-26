using Content.Shared.DisplacementMap;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Serialization.Manager;

namespace Content.Client.DisplacementMap;

public sealed class DisplacementMapSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <summary>
    /// Attempting to apply a displacement map to a specific layer of SpriteComponent
    /// </summary>
    /// <param name="data">Information package for applying the displacement map</param>
    /// <param name="sprite">Entity with SpriteComponent</param>
    /// <param name="index">Index of the layer where the new map layer will be added</param>
    /// <param name="key">Unique layer key, which will determine which layer to apply displacement map to</param>
    /// <param name="displacementKey">The key of the new displacement map layer added by this function.</param>
    /// <returns></returns>
    public bool TryAddDisplacement(DisplacementData data,
        Entity<SpriteComponent> sprite,
        int index,
        object key,
        out string displacementKey)
    {
        displacementKey = $"{key}-displacement";

        if (key.ToString() is null)
            return false;

        if (data.ShaderOverride != null)
            sprite.Comp.LayerSetShader(index, data.ShaderOverride);

        _sprite.RemoveLayer(sprite.AsNullable(), displacementKey, false);

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
        var actualRSI = _sprite.LayerGetEffectiveRsi(sprite.AsNullable(), index);
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

        _sprite.AddLayer(sprite.AsNullable(), displacementLayer, index);
        _sprite.LayerMapSet(sprite.AsNullable(), displacementKey, index);

        return true;
    }

    /// <inheritdoc cref="TryAddDisplacement"/>
    [Obsolete("Use the Entity<SpriteComponent> overload")]
    public bool TryAddDisplacement(DisplacementData data,
        SpriteComponent sprite,
        int index,
        object key,
        out string displacementKey)
    {
        return TryAddDisplacement(data, (sprite.Owner, sprite), index, key, out displacementKey);
    }
}
