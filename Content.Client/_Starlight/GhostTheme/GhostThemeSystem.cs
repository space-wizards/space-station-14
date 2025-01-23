using Content.Shared.Starlight.GhostTheme;
using Content.Shared.Starlight;
using Content.Shared.Ghost;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Client.Starlight.GhostTheme;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayersRoleManager _playerManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostThemeComponent, AppearanceChangeEvent>(OnAppearance);
    }
    
    private void OnAppearance(Entity<GhostThemeComponent> ent, ref AppearanceChangeEvent args)
    {
        SyncTheme(ent.Owner, ent.Comp);
    }
    
    private void SyncTheme(EntityUid uid, GhostThemeComponent component)
    {
        if (!_appearance.TryGetData<string>(uid, GhostThemeVisualLayers.Base, out var Theme)
            || !_prototypeManager.TryIndex<GhostThemePrototype>(Theme, out var ghostThemePrototype) 
            || !EntityManager.TryGetComponent<SpriteComponent>(uid, out var sprite)
            || sprite.LayerMapTryGet(EffectLayers.Unshaded, out var layer))
            return;

        sprite.LayerSetSprite(layer, ghostThemePrototype.SpriteSpecifier.Sprite);
        sprite.LayerSetShader(layer, "unshaded");
        sprite.LayerSetColor(layer, ghostThemePrototype.SpriteSpecifier.SpriteColor);
        sprite.LayerSetScale(layer, ghostThemePrototype.SpriteSpecifier.SpriteScale);
        sprite.NoRotation = ghostThemePrototype.SpriteSpecifier.SpriteRotation;

        sprite.DrawDepth = DrawDepth.Default + 11;
        sprite.OverrideContainerOcclusion = true;
    }
}