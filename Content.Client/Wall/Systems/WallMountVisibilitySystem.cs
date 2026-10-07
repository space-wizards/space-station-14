using Content.Shared.CCVar;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;

namespace Content.Client.Wall.Systems;

/// <summary>
/// Manages the directional visibility overlay for wall-mounted entities.
/// </summary>
public sealed partial class WallMountVisibilitySystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<WallComponent> _wallQuery;

    private WallMountVisibilityOverlay _overlayInstance = default!;

    /// <summary>
    /// Whether directional visibility is currently enabled.
    /// </summary>
    public bool DirectionalVisibilityEnabled = true;

    /// <summary>
    /// Whether wall-mount visibility changes fade smoothly or snap instantly.
    /// </summary>
    public bool FadeEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _overlayInstance = new WallMountVisibilityOverlay();

        Subs.CVar(_cfg, CCVars.WallMountDirectionalVisibility, OnDirectionalVisibilityChanged, true);
        Subs.CVar(_cfg, CCVars.WallMountFade, OnFadeChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_overlayInstance);
    }

    private void OnDirectionalVisibilityChanged(bool enabled)
    {
        DirectionalVisibilityEnabled = enabled;

        if (enabled)
            _overlay.AddOverlay(_overlayInstance);
        else
        {
            _overlay.RemoveOverlay(_overlayInstance);
            _overlayInstance.RestoreAll();
        }
    }

    private void OnFadeChanged(bool enabled)
    {
        FadeEnabled = enabled;
    }

    /// <summary>
    /// Makes the entity visible again on component shutdown.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWallMountShutdown(Entity<WallMountComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    /// <summary>
    /// Makes the entity visible again if directional visibility is disabled for this mount.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWallMountAfterHandleState(Entity<WallMountComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.DirectionalVisibility)
            return;

        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    /// <summary>
    /// Checks whether the tile contains any anchored blocking entity.
    /// </summary>
    public bool IsTileBlocked(Entity<MapGridComponent> grid, Vector2i tile)
    {
        var enumerator = _map.GetAnchoredEntities(grid.Owner, grid, tile);
        while (enumerator.MoveNext(out var anchored))
        {
            if (!_wallQuery.HasComp(anchored.Value))
                continue;

            return true;
        }
        return false;
    }
}
