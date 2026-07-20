using Content.Shared.CCVar;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Wall.Systems;

/// <summary>
/// Manages the directional visibility overlay for wall-mounted entities.
/// </summary>
public sealed partial class WallMountVisibilitySystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private WallMountTreeSystem _tree = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    /// <summary>
    /// Tags that block visibility when present on the same tile.
    /// </summary>
    private static readonly ProtoId<TagPrototype>[] BlockingTags = ["Wall"];

    private WallMountVisibilityOverlay _overlayInstance = default!;

    /// <summary>
    /// Whether directional visibility is currently enabled.
    /// </summary>
    public bool DirectionalVisibilityEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _overlayInstance = new WallMountVisibilityOverlay(_timing, _map, _sprite, _xform, _tree, this, _gridQuery, _spriteQuery);
        Subs.CVar(_cfg, CCVars.WallMountDirectionalVisibility, OnDirectionalVisibilityChanged, true);
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
            SetAllVisible(true);
        }
    }

    /// <summary>
    /// Makes the entity visible again on component shutdown.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWallMountShutdown(Entity<WallMountComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
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

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetVisible((ent, sprite), true);
    }

    /// <summary>
    /// Forces all wall-mount entities to become visible or hidden.
    /// </summary>
    public void SetAllVisible(bool visible)
    {
        var query = AllEntityQuery<WallMountComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            _sprite.SetVisible((uid, sprite), visible);
        }
    }

    /// <summary>
    /// Checks whether the tile contains any anchored blocking entity.
    /// </summary>
    public bool IsTileBlocked(Entity<MapGridComponent> grid, Vector2i tile)
    {
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid.Owner, grid, tile);
        while (enumerator.MoveNext(out var anchored))
        {
            if (!_tag.HasAnyTag(anchored.Value, BlockingTags))
                continue;

            return true;
        }
        return false;
    }
}
