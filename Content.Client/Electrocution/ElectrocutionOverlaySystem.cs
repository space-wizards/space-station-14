using Content.Shared.Electrocution;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Electrocution;

/// <summary>
/// Shows the ElectrocutionOverlay to entities with the ElectrocutionOverlayComponent.
/// </summary>
public sealed class ElectrocutionOverlaySystem : EntitySystem
{

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ElectrocutionOverlayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ElectrocutionOverlayComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ElectrocutionOverlayComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ElectrocutionOverlayComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ElectrifiedComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnPlayerAttached(Entity<ElectrocutionOverlayComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        ShowOverlay();
    }

    private void OnPlayerDetached(Entity<ElectrocutionOverlayComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void OnInit(Entity<ElectrocutionOverlayComponent> ent, ref ComponentInit args)
    {
        if (_playerMan.LocalEntity == ent)
        {
            ShowOverlay();
        }
    }

    private void OnShutdown(Entity<ElectrocutionOverlayComponent> ent, ref ComponentShutdown args)
    {
        if (_playerMan.LocalEntity == ent)
        {
            RemoveOverlay();
        }
    }

    private void ShowOverlay()
    {
        var electrifiedQuery = AllEntityQuery<ElectrifiedComponent, AppearanceComponent, SpriteComponent>();
        while (electrifiedQuery.MoveNext(out var uid, out var _, out var appearanceComp, out var spriteComp))
        {
            if (!_appearance.TryGetData<bool>(uid, ElectrifiedVisuals.IsElectrified, out var electrified, appearanceComp))
                continue;

            if (!spriteComp.LayerMapTryGet(ElectrifiedLayers.Overlay, out var layer))
                continue;

            if (electrified)
                spriteComp.LayerSetVisible(ElectrifiedLayers.Overlay, true);
            else
                spriteComp.LayerSetVisible(ElectrifiedLayers.Overlay, false);
        }
    }

    private void RemoveOverlay()
    {
        var electrifiedQuery = AllEntityQuery<ElectrifiedComponent, AppearanceComponent, SpriteComponent>();
        while (electrifiedQuery.MoveNext(out var uid, out var _, out var appearanceComp, out var spriteComp))
        {
            if (!spriteComp.LayerMapTryGet(ElectrifiedLayers.Overlay, out var layer))
                continue;

            spriteComp.LayerSetVisible(layer, false);
        }
    }

    private void OnAppearanceChange(Entity<ElectrifiedComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(ent.Owner, ElectrifiedVisuals.IsElectrified, out var electrified, args.Component))
            return;

        if (!args.Sprite.LayerMapTryGet(ElectrifiedLayers.Overlay, out var layer))
            return;

        var player = _playerMan.LocalEntity;
        if (electrified && HasComp<ElectrocutionOverlayComponent>(player))
            args.Sprite.LayerSetVisible(layer, true);
        else
            args.Sprite.LayerSetVisible(layer, false);
    }
}
