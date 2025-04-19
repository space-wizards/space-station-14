using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// The system responsible for updating the appearance of layered gas pipe
/// </summary>
public sealed partial class AtmosPipeLayersSystem : SharedAtmosPipeLayersSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<AtmosPipeLayersComponent> ent, ref AppearanceChangeEvent ev)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_appearance.TryGetData<int>(ent, AtmosPipeLayerVisuals.DrawDepth, out var drawDepth))
        {
            sprite.DrawDepth = drawDepth;
        }

        if (_appearance.TryGetData<string>(ent, AtmosPipeLayerVisuals.Sprite, out var spriteRsi) &&
            _resourceCache.TryGetResource(SpriteSpecifierSerializer.TextureRoot / spriteRsi, out RSIResource? resource))
        {
            sprite.BaseRSI = resource.RSI;
        }

        if (_appearance.TryGetData<Dictionary<string, string>>(ent, AtmosPipeLayerVisuals.SpriteLayers, out var pipeState))
        {
            foreach (var (layerKey, rsiPath) in pipeState)
                sprite.LayerSetRSI(ParseKey(layerKey), rsiPath);
        }
    }

    /// <summary>
    /// Parses a string for enum references
    /// </summary>
    /// <param name="keyString">The string to parse</param>
    /// <returns>The parsed string</returns>
    private object ParseKey(string keyString)
    {
        if (_reflection.TryParseEnumReference(keyString, out var @enum))
            return @enum;

        return keyString;
    }
}
