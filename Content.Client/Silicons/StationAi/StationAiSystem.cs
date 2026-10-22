using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem : SharedStationAiSystem
{
    [Dependency] private IOverlayManager _overlayMgr = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private StationAiOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();
        InitializeAirlock();
        InitializePowerToggle();
    }

    [SubscribeLocalEvent]
    private void OnAiOverlayInit(Entity<StationAiOverlayComponent> ent, ref ComponentInit args)
    {
        var attachedEnt = _player.LocalEntity;

        if (attachedEnt != ent.Owner)
            return;

        AddOverlay();
    }

    [SubscribeLocalEvent]
    private void OnAiOverlayRemove(Entity<StationAiOverlayComponent> ent, ref ComponentRemove args)
    {
        var attachedEnt = _player.LocalEntity;

        if (attachedEnt != ent.Owner)
            return;

        RemoveOverlay();
    }

    private void AddOverlay()
    {
        if (_overlay != null)
            return;

        _overlay = new StationAiOverlay();
        _overlayMgr.AddOverlay(_overlay);
    }

    private void RemoveOverlay()
    {
        if (_overlay == null)
            return;

        _overlayMgr.RemoveOverlay(_overlay);
        _overlay = null;
    }

    [SubscribeLocalEvent]
    private void OnAiAttached(Entity<StationAiOverlayComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    [SubscribeLocalEvent]
    private void OnAiDetached(Entity<StationAiOverlayComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<StationAiCoreComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.TryGetData<PrototypeLayerData>(StationAiVisualLayers.Icon, out var layerData))
            _sprite.LayerSetData((entity.Owner, args.Sprite), StationAiVisualLayers.Icon, layerData);

        _sprite.LayerSetVisible((entity.Owner, args.Sprite), StationAiVisualLayers.Icon, layerData != null);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMgr.RemoveOverlay<StationAiOverlay>();
    }
}
