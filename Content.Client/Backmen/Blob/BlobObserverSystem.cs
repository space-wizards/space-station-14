using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Graphics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Backmen.Blob;

public sealed class BlobObserverSystem : SharedBlobObserverSystem
{
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobObserverComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BlobObserverComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<BlobCarrierComponent, GetStatusIconsEvent>(GetBlobCarrierIcon);
        SubscribeLocalEvent<BlobObserverComponent, GetStatusIconsEvent>(GetBlobObserverIcon);
        SubscribeLocalEvent<ZombieBlobComponent, GetStatusIconsEvent>(GetZombieBlobIcon);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);
    }

    private void GetBlobCarrierIcon(Entity<BlobCarrierComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetBlobObserverIcon(Entity<BlobObserverComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetZombieBlobIcon(Entity<ZombieBlobComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    /// <summary>
    /// The criteria that determine whether a client should see Rev/Head rev icons.
    /// </summary>
    private bool CanDisplayIcon(EntityUid? uid, bool visibleToGhost)
    {
        if (HasComp<BlobCarrierComponent>(uid))
            return true;

        if (HasComp<ZombieBlobComponent>(uid))
            return true;

        if (visibleToGhost && HasComp<GhostComponent>(uid))
            return true;

        return HasComp<BlobObserverComponent>(uid);
    }

    private void OnPlayerAttached(EntityUid uid, BlobObserverComponent component, LocalPlayerAttachedEvent args)
    {
        _lightManager.DrawLighting = false;
    }

    private void OnPlayerDetached(EntityUid uid, BlobObserverComponent component, LocalPlayerDetachedEvent args)
    {
        _lightManager.DrawLighting = true;
    }

    private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _lightManager.DrawLighting = true;
    }
}
