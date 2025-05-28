using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// The system responsible for updating the appearance of layered gas pipe
/// </summary>
public sealed partial class AtmosPipeLayersSystem : SharedAtmosPipeLayersSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosPipeLayersComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<AtmosPipeLayersComponent> ent, ref AppearanceChangeEvent ev)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_appearance.TryGetData<string>(ent, AtmosPipeLayerVisuals.Sprite, out var spriteRsi) &&
            _resourceCache.TryGetResource(SpriteSpecifierSerializer.TextureRoot / spriteRsi, out RSIResource? resource))
        {
            _sprite.SetBaseRsi((ent, sprite), resource.RSI);
        }

        if (_appearance.TryGetData<Dictionary<string, string>>(ent, AtmosPipeLayerVisuals.SpriteLayers, out var pipeState))
        {
            foreach (var (layerKey, rsiPath) in pipeState)
            {
                _sprite.LayerSetRsi((ent, sprite), layerKey, new ResPath(rsiPath));
            }
        }
    }
}
