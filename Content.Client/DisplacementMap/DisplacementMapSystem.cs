using System.Diagnostics.CodeAnalysis;
using Content.Shared.DisplacementMap;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Client.DisplacementMap;

public sealed class DisplacementMapSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = null!;
    [Dependency] private readonly SpriteSystem _sprite = null!;

    //needs to be replaced later: see comment on line 48
    private static readonly ProtoId<ShaderPrototype> UnshadedID = "unshaded";

    private static string? BuildDisplacementLayerKey(object key)
    {
        return key.ToString() is null ? null : $"{key}-displacement";
    }

    /// <summary>
    /// Attempting to apply a displacement map to a specific layer of SpriteComponent
    /// </summary>
    /// <param name="data">Information package for applying the displacement map</param>
    /// <param name="sprite">Entity with SpriteComponent</param>
    /// <param name="index">Index of the layer where the new map layer will be added</param>
    /// <param name="key">Unique layer key, which will determine which layer to apply displacement map to</param>
    /// <param name="displacementKey">The key of the new displacement map layer added by this function.</param>
    /// <returns></returns>
    public bool TryAddDisplacement(
        DisplacementData data,
        Entity<SpriteComponent> sprite,
        int index,
        object key,
        [NotNullWhen(true)] out string? displacementKey
    )
    {
        displacementKey = BuildDisplacementLayerKey(key);
        if (displacementKey is null)
            return false;

        EnsureDisplacementIsNotOnSprite(sprite, key);

        if (data.ShaderOverride is not null)
        {
            //TODO : this is a kinda janky workaround for the fact that the current rendering pipeline does not have
            //proper support for multiple shaders on a given layer (or an ubershader to handle stacking all of the effects well)
            //should be replaced by an engine-level solution, but this is an adequate temporary solution.
            //what's that phrase about temporary solutions?
            sprite.Comp.LayerSetShader(index,
                (sprite.Comp[index] is SpriteComponent.Layer layer && layer.ShaderPrototype == UnshadedID)
                    ? data.ShaderOverrideUnshaded
                    : data.ShaderOverride);
        }

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

        // This previously assigned a string reading "this is impossible" if key.ToString eval'd to false.
        // However, for the sake of sanity, we've changed this to assert non-null - !.
        // If this throws an error, we're not sorry. Nanotrasen thanks you for your service fixing this bug.
        displacementLayer.CopyToShaderParameters!.LayerKey = key.ToString()!;

        _sprite.AddLayer(sprite.AsNullable(), displacementLayer, index);
        _sprite.LayerMapSet(sprite.AsNullable(), displacementKey, index);

        return true;
    }

    /// <summary>
    /// Ensures that the displacement map associated with the given layer key is not in the Sprite's LayerMap.
    /// </summary>
    /// <param name="sprite">The sprite to remove the displacement layer from.</param>
    /// <param name="key">The key of the layer that is referenced by the displacement layer we want to remove.</param>
    /// <param name="logMissing">Whether to report an error if the displacement map isn't on the sprite.</param>
    public void EnsureDisplacementIsNotOnSprite(Entity<SpriteComponent> sprite, object key)
    {
        var displacementLayerKey = BuildDisplacementLayerKey(key);
        if (displacementLayerKey is null)
            return;

        _sprite.RemoveLayer(sprite.AsNullable(), displacementLayerKey, false);
    }
}
