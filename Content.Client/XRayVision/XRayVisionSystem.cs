using Content.Client.Overlays;
using Content.Shared.GameTicking;
using Content.Shared.XRayVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.XRayVision;

/// <inheritdoc/>
public sealed partial class XRayVisionSystem : SharedXRayVisionSystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IPlayerManager _player = default!;

    private XRayVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new XRayVisionOverlay();
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RefreshOverlay(args.Entity);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        Deactivate(_player.LocalEntity);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<XRayVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay(ent);
    }

    [SubscribeNetworkEvent]
    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        var localPlayer = _player.LocalSession?.AttachedEntity;
        if (localPlayer != null)
            Deactivate(localPlayer.Value);
    }

    private void Update(EntityUid entity, List<Entity<XRayVisionComponent>> entities)
    {
        if (entity != _player.LocalSession?.AttachedEntity)
            return;

        // Find the first active xray component.
        XRayVisionComponent? xray = null;
        foreach (var ent in entities)
        {
            if (!ent.Comp.Enabled)
                continue;

            if (ent.Comp.RelayOverlay == (ent.Owner == entity))
                continue;

            xray ??= ent.Comp;
        }

        // There is no active xray components, so we disable the overlay.
        if (xray == null)
        {
            Deactivate(entity);
            return;
        }

        _overlay.SetParameters(xray.TileOverlayColor, xray.EntityOverlayColor, xray.ShowTiles, xray.ScanlinesIntensity);

        if (!_overlayMan.HasOverlay<XRayVisionOverlay>())
            _overlayMan.AddOverlay(_overlay);
    }

    private void Deactivate(EntityUid? ent)
    {
        if (ent != _player.LocalSession?.AttachedEntity)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    protected override void RefreshOverlay(EntityUid target)
    {
        if (target != _player.LocalSession?.AttachedEntity)
            return;

        var ev = new RefreshXRayVisionEvent();
        RaiseLocalEvent(target, ref ev);

        if (ev.Entities.Count > 0)
            Update(target, ev.Entities);
        else
            Deactivate(target);
    }
}
