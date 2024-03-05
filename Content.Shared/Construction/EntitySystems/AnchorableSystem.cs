using Content.Shared.Administration.Logs;
using Content.Shared.Examine;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Shared.Construction.EntitySystems;

public sealed partial class AnchorableSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPullingSystem _pulling = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private   readonly TagSystem _tagSystem = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TagComponent> _tagQuery;

    public const string Unstackable = "Unstackable";

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _tagQuery = GetEntityQuery<TagComponent>();

        SubscribeLocalEvent<AnchorableComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ItemSlotsSystem) }, after: new[] { typeof(SharedConstructionSystem) });
        SubscribeLocalEvent<AnchorableComponent, TryAnchorCompletedEvent>(OnAnchorComplete);
        SubscribeLocalEvent<AnchorableComponent, TryUnanchorCompletedEvent>(OnUnanchorComplete);
        SubscribeLocalEvent<AnchorableComponent, ExaminedEvent>(OnAnchoredExamine);
    }

    /// <summary>
    ///     Tries to unanchor the entity.
    /// </summary>
    /// <returns>true if unanchored, false otherwise</returns>
    private void TryUnAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return;

        if (!Resolve(usingUid, ref usingTool))
            return;

        if (!Valid(uid, userUid, usingUid, false))
            return;

        // Log unanchor attempt (server only)
        _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to unanchor {ToPrettyString(uid):entity} from {transform.Coordinates:targetlocation}");

        _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryUnanchorCompletedEvent());
    }

    private void OnInteractUsing(EntityUid uid, AnchorableComponent anchorable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // If the used entity doesn't have a tool, return early.
        if (!TryComp(args.Used, out ToolComponent? usedTool) || !usedTool.Qualities.Contains(anchorable.Tool))
            return;

        args.Handled = true;
        TryToggleAnchor(uid, args.User, args.Used, anchorable, usingTool: usedTool);
    }

    private void OnAnchoredExamine(EntityUid uid, AnchorableComponent component, ExaminedEvent args)
    {
        var isAnchored = Comp<TransformComponent>(uid).Anchored;
        var messageId = isAnchored ? "examinable-anchored" : "examinable-unanchored";
        args.PushMarkup(Loc.GetString(messageId, ("target", uid)));
    }

    private void OnUnanchorComplete(EntityUid uid, AnchorableComponent component, TryUnanchorCompletedEvent args)
    {
        if (args.Cancelled || args.Used is not { } used)
            return;

        var xform = Transform(uid);

        RaiseLocalEvent(uid, new BeforeUnanchoredEvent(args.User, used));
        _transformSystem.Unanchor(uid, xform);
        RaiseLocalEvent(uid, new UserUnanchoredEvent(args.User, used));

        _popup.PopupClient(Loc.GetString("anchorable-unanchored"), uid, args.User);

        _adminLogger.Add(
            LogType.Unanchor,
            LogImpact.Low,
            $"{EntityManager.ToPrettyString(args.User):user} unanchored {EntityManager.ToPrettyString(uid):anchored} using {EntityManager.ToPrettyString(used):using}"
        );
    }

    private void OnAnchorComplete(EntityUid uid, AnchorableComponent component, TryAnchorCompletedEvent args)
    {
        if (args.Cancelled || args.Used is not { } used)
            return;

        var xform = Transform(uid);
        if (TryComp<PhysicsComponent>(uid, out var anchorBody) &&
            !TileFree(xform.Coordinates, anchorBody))
        {
            _popup.PopupClient(Loc.GetString("anchorable-occupied"), uid, args.User);
            return;
        }

        // Snap rotation to cardinal (multiple of 90)
        var rot = xform.LocalRotation;
        xform.LocalRotation = Math.Round(rot / (Math.PI / 2)) * (Math.PI / 2);

        if (TryComp<SharedPullableComponent>(uid, out var pullable) && pullable.Puller != null)
        {
            _pulling.TryStopPull(pullable);
        }

        // TODO: Anchoring snaps rn anyway!
        if (component.Snap)
        {
            var coordinates = xform.Coordinates.SnapToGrid(EntityManager, _mapManager);

            if (AnyUnstackable(uid, coordinates))
            {
                _popup.PopupClient(Loc.GetString("construction-step-condition-no-unstackable-in-tile"), uid, args.User);
                return;
            }

            _transformSystem.SetCoordinates(uid, coordinates);
        }

        RaiseLocalEvent(uid, new BeforeAnchoredEvent(args.User, used));

        if (!xform.Anchored)
            _transformSystem.AnchorEntity(uid, xform);

        RaiseLocalEvent(uid, new UserAnchoredEvent(args.User, used));

        _popup.PopupClient(Loc.GetString("anchorable-anchored"), uid, args.User);

        _adminLogger.Add(
            LogType.Anchor,
            LogImpact.Low,
            $"{EntityManager.ToPrettyString(args.User):user} anchored {EntityManager.ToPrettyString(uid):anchored} using {EntityManager.ToPrettyString(used):using}"
        );
    }

    /// <summary>
    ///     Tries to toggle the anchored status of this component's owner.
    ///     override is used due to popup and adminlog being server side systems in this case.
    /// </summary>
    /// <returns>true if toggled, false otherwise</returns>
    public void TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        SharedPullableComponent? pullable = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref transform))
            return;

        if (transform.Anchored)
        {
            TryUnAnchor(uid, userUid, usingUid, anchorable, transform, usingTool);
        }
        else
        {
            TryAnchor(uid, userUid, usingUid, anchorable, transform, pullable, usingTool);
        }
    }

    /// <summary>
    ///     Tries to anchor the entity.
    /// </summary>
    /// <returns>true if anchored, false otherwise</returns>
    private void TryAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
            AnchorableComponent? anchorable = null,
            TransformComponent? transform = null,
            SharedPullableComponent? pullable = null,
            ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return;

        // Optional resolves.
        Resolve(uid, ref pullable, false);

        if (!Resolve(usingUid, ref usingTool))
            return;

        if (!Valid(uid, userUid, usingUid, true, anchorable, usingTool))
            return;

        // Log anchor attempt (server only)
        _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to anchor {ToPrettyString(uid):entity} to {transform.Coordinates:targetlocation}");

        if (TryComp<PhysicsComponent>(uid, out var anchorBody) &&
            !TileFree(transform.Coordinates, anchorBody))
        {
            _popup.PopupClient(Loc.GetString("anchorable-occupied"), uid, userUid);
            return;
        }

        if (AnyUnstackable(uid, transform.Coordinates))
        {
            _popup.PopupClient(Loc.GetString("construction-step-condition-no-unstackable-in-tile"), uid, userUid);
            return;
        }

        _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryAnchorCompletedEvent());
    }

    private bool Valid(
        EntityUid uid,
        EntityUid userUid,
        EntityUid usingUid,
        bool anchoring,
        AnchorableComponent? anchorable = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable))
            return false;

        if (!Resolve(usingUid, ref usingTool))
            return false;

        if (anchoring && (anchorable.Flags & AnchorableFlags.Anchorable) == 0x0)
            return false;

        if (!anchoring && (anchorable.Flags & AnchorableFlags.Unanchorable) == 0x0)
            return false;

        BaseAnchoredAttemptEvent attempt =
            anchoring ? new AnchorAttemptEvent(userUid, usingUid) : new UnanchorAttemptEvent(userUid, usingUid);

        // Need to cast the event or it will be raised as BaseAnchoredAttemptEvent.
        if (anchoring)
            RaiseLocalEvent(uid, (AnchorAttemptEvent) attempt);
        else
            RaiseLocalEvent(uid, (UnanchorAttemptEvent) attempt);

        anchorable.Delay += attempt.Delay;

        return !attempt.Cancelled;
    }

    private bool TileFree(EntityCoordinates coordinates, PhysicsComponent anchorBody)
    {
        // Probably ignore CanCollide on the anchoring body?
        var gridUid = coordinates.GetGridUid(EntityManager);

        if (!_mapManager.TryGetGrid(gridUid, out var grid))
            return false;

        var tileIndices = grid.TileIndicesFor(coordinates);
        return TileFree(grid, tileIndices, anchorBody.CollisionLayer, anchorBody.CollisionMask);
    }

    /// <summary>
    /// Returns true if no hard anchored entities match the collision layer or mask specified.
    /// </summary>
    /// <param name="grid"></param>
    public bool TileFree(MapGridComponent grid, Vector2i gridIndices, int collisionLayer = 0, int collisionMask = 0)
    {
        var enumerator = grid.GetAnchoredEntitiesEnumerator(gridIndices);

        while (enumerator.MoveNext(out var ent))
        {
            if (!_physicsQuery.TryGetComponent(ent, out var body) ||
                !body.CanCollide ||
                !body.Hard)
            {
                continue;
            }

            if ((body.CollisionMask & collisionLayer) != 0x0 ||
                (body.CollisionLayer & collisionMask) != 0x0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if any unstackables are also on the corresponding tile.
    /// </summary>
    public bool AnyUnstackable(EntityUid uid, EntityCoordinates location)
    {
        DebugTools.Assert(!Transform(uid).Anchored);

        // If we are unstackable, iterate through any other entities anchored on the current square
        return _tagSystem.HasTag(uid, Unstackable, _tagQuery) && AnyUnstackablesAnchoredAt(location);
    }

    public bool AnyUnstackablesAnchoredAt(EntityCoordinates location)
    {
        var gridUid = location.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var enumerator = grid.GetAnchoredEntitiesEnumerator(grid.LocalToTile(location));

        while (enumerator.MoveNext(out var entity))
        {
            // If we find another unstackable here, return true.
            if (_tagSystem.HasTag(entity.Value, Unstackable, _tagQuery))
            {
                return true;
            }
        }

        return false;
    }

    [Serializable, NetSerializable]
    private sealed partial class TryUnanchorCompletedEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    private sealed partial class TryAnchorCompletedEvent : SimpleDoAfterEvent
    {
    }
}
