using Content.Shared.Silicons.StationAi;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Silicons.StationAi;

public sealed partial class StationAiSystem : SharedStationAiSystem
{
    [Dependency] private readonly IOverlayManager _overlayMgr = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private StationAiOverlay? _overlay;
    private EntityUid? _overlayOwner; // DS14
    private (EntityUid Entity, bool WasVisible)? _hiddenRemoteEye; // DS14

    public override void Initialize()
    {
        base.Initialize();
        InitializeAirlock();
        InitializePowerToggle();

        // DS14-start
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<StationAiOverlayComponent, ComponentStartup>(OnAiOverlayStartup);
        SubscribeLocalEvent<StationAiOverlayComponent, ComponentShutdown>(OnAiOverlayShutdown);
        // DS14-end
        SubscribeLocalEvent<StationAiCoreComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    // DS14-start
    private void OnAiOverlayStartup(Entity<StationAiOverlayComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        EnsureOverlay(ent.Owner);
    }

    private void OnAiOverlayShutdown(Entity<StationAiOverlayComponent> ent, ref ComponentShutdown args)
    {
        if (_overlayOwner == ent.Owner)
            RemoveOverlay();
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RemoveOverlay();

        if (HasComp<StationAiOverlayComponent>(args.Entity))
            EnsureOverlay(args.Entity);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_overlayOwner == args.Entity)
            RemoveOverlay();
    }

    private void EnsureOverlay(EntityUid owner)
    {
        if (_overlay != null && _overlayOwner == owner)
            return;

        RemoveOverlay();
        _overlayOwner = owner;
        _overlay = new StationAiOverlay(owner);
        _overlayMgr.AddOverlay(_overlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_overlayOwner is not { } owner ||
            !TryGetCore(owner, out var core) ||
            core.Comp is not { Remote: true, RemoteEntity: { } remoteEye } ||
            !TryComp(remoteEye, out SpriteComponent? sprite))
        {
            RestoreRemoteEye();
            return;
        }

        if (_hiddenRemoteEye is { } hidden && hidden.Entity == remoteEye)
        {
            if (sprite.Visible)
                _sprite.SetVisible((remoteEye, sprite), false);

            return;
        }

        RestoreRemoteEye();
        _hiddenRemoteEye = (remoteEye, sprite.Visible);
        _sprite.SetVisible((remoteEye, sprite), false);
    }

    private void RemoveOverlay()
    {
        RestoreRemoteEye();

        if (_overlay == null)
        {
            _overlayOwner = null;
            return;
        }

        _overlayMgr.RemoveOverlay(_overlay);
        _overlay.Dispose();
        _overlay = null;
        _overlayOwner = null;
    }

    private void RestoreRemoteEye()
    {
        if (_hiddenRemoteEye is not { } hidden)
            return;

        if (TryComp(hidden.Entity, out SpriteComponent? sprite))
            _sprite.SetVisible((hidden.Entity, sprite), hidden.WasVisible);

        _hiddenRemoteEye = null;
    }
    // DS14-end

    private void OnAppearanceChange(Entity<StationAiCoreComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<PrototypeLayerData>(entity.Owner, StationAiVisualLayers.Icon, out var layerData, args.Component))
            _sprite.LayerSetData((entity.Owner, args.Sprite), StationAiVisualLayers.Icon, layerData);

        _sprite.LayerSetVisible((entity.Owner, args.Sprite), StationAiVisualLayers.Icon, layerData != null);
    }

    public override void Shutdown()
    {
        RemoveOverlay(); // DS14
        base.Shutdown();
    }
}
