using Content.Shared.Mining.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Mining;

/// <summary>
/// This handles...
/// </summary>
public sealed class MiningOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private MiningOverlay _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MiningScannerViewerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MiningScannerViewerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MiningScannerViewerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MiningScannerViewerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(Entity<MiningScannerViewerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<MiningScannerViewerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.RequestScreenTexture = false;
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnInit(Entity<MiningScannerViewerComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent)
        {
            _overlay.RequestScreenTexture = true;
            _overlayMan.AddOverlay(_overlay);
        }
    }

    private void OnShutdown(Entity<MiningScannerViewerComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent)
        {
            _overlay.RequestScreenTexture = false;
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
