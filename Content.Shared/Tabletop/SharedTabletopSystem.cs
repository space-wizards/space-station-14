using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.CCVar;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Tabletop;

public abstract partial class SharedTabletopSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] protected MetaDataSystem Meta = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedTransformSystem Xform = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private SharedViewSubscriberSystem _viewSubscriber = default!;

    [Dependency] protected EntityQuery<ActorComponent> ActorQuery;
    [Dependency] protected EntityQuery<AppearanceComponent> AppearanceQuery;
    [Dependency] protected EntityQuery<TabletopBackgroundComponent> BackgroundQuery;
    [Dependency] protected EntityQuery<TabletopDraggableComponent> DraggableQuery;
    [Dependency] protected EntityQuery<TabletopGameComponent> GameQuery;
    [Dependency] protected EntityQuery<TabletopGamerComponent> GamerQuery;

    /// <summary>
    /// The prototype to use to represent items dragged into the tabletop map.
    /// </summary>
    protected static readonly EntProtoId GamePiecePrototype = "BaseTabletopPiece";

    /// <summary>
    /// The maximum number of pieces to allow placement on a table.
    /// </summary>
    protected static readonly int MaxTabletopPieces = 50;

    /// <summary>
    /// Creates a sanitized copy of an entity and sends it into a particular tabletop game.
    /// </summary>
    /// <param name="target">The entity to copy.</param>
    /// <param name="ent">The tabletop game to send the piece into.</param>
    /// <param name="user">The user to show a popup on </param>
    protected abstract void CopyEntity(EntityUid target, Entity<TabletopGameComponent> ent, EntityUid user);

    /// <summary>
    /// An event to open a window.
    /// </summary>
    /// <param name="tableUid">The table entity.</param>
    /// <param name="cameraUid">The camera to follow.</param>
    /// <param name="title">The title of the window.</param>
    /// <param name="size">The size of the window.</param>
    protected virtual void OnTabletopPlay(EntityUid tableUid, EntityUid cameraUid, string title, Vector2i size) { }

    #region Event Handlers

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<TabletopGameComponent> ent, ref InteractUsingEvent args)
    {
        if (!_cfg.GetCVar(CCVars.GameTabletopPlace))
            return;

        if (!ent.Comp.HasSession)
            return;

        if (!_hands.TryGetActiveItem(args.User, out var maybeHandEnt) || maybeHandEnt is not { } handEnt)
            return;

        if (!HasComp<ItemComponent>(handEnt))
            return;

        CopyEntity(handEnt, ent, args.User);
    }

    [SubscribeLocalEvent]
    private void OnTabletopBoundUIOpened(Entity<TabletopGameComponent> ent, ref BoundUIOpenedEvent args)
    {
        // Check that a player is attached to the entity.
        if (!ActorQuery.TryComp(args.Actor, out ActorComponent? actor))
            return;

        OpenSessionFor(actor.PlayerSession, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void AddDraggableCopyVerb(Entity<TabletopDraggableComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!GamerQuery.TryComp(args.User, out var gamer)
            || !GameQuery.TryComp(gamer.Tabletop, out var game))
            return;

        // Will get closed later if CanSeeTable returns false.
        var disabled = !CanSeeTable(args.User, gamer.Tabletop);
        var user = args.User;

        var copyVerb = new AlternativeVerb()
        {
            Text = Loc.GetString("tabletop-verb-copy-piece"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/flip.svg.192dpi.png")),
            Act = () => CopyEntity(entity, (gamer.Tabletop, game), user),
            Disabled = disabled,
            Message = Loc.GetString(disabled ? "tabletop-verb-copy-piece-message-disabled" : "tabletop-verb-copy-piece-message")
        };

        args.Verbs.Add(copyVerb);
    }

    [SubscribeLocalEvent]
    private void AddHologramRemoveVerb(Entity<TabletopHologramComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!GamerQuery.TryComp(args.User, out var gamer)
            || !GameQuery.TryComp(gamer.Tabletop, out var game))
            return;

        // Will get closed later if CanSeeTable returns false.
        var disabled = !CanSeeTable(args.User, gamer.Tabletop);
        var user = args.User;

        var removeVerb = new Verb()
        {
            Text = Loc.GetString("tabletop-verb-remove-piece"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete.svg.192dpi.png")),
            Act = () => RemovePiece(entity, (gamer.Tabletop, game), user),
            Disabled = disabled,
            Priority = 1,
            Message = Loc.GetString(disabled ? "tabletop-verb-remove-piece-message-disabled" : "tabletop-verb-remove-piece-message")
        };

        args.Verbs.Add(removeVerb);
    }

    /// <summary>
    /// Move an entity which is dragged by the user, but check if they are allowed to do so and to these coordinates.
    /// </summary>
    [EventSubscription] // Both local and networked events
    protected virtual void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { AttachedEntity: { } playerEntity })
            return;

        var table = GetEntity(msg.TableUid);
        var moved = GetEntity(msg.MovedEntityUid);

        if (!CanSeeTable(playerEntity, table) || !CanDrag(playerEntity, moved, out _))
            return;

        // Move the entity and dirty it (we use the map ID from the entity so noone can try to be funny and move the item to another map)
        var transform = Comp<TransformComponent>(moved);
        Xform.SetParent(moved, transform, transform.MapUid ?? EntityUid.Invalid);
        Xform.SetLocalPosition(moved, msg.Coordinates.Position, transform);
    }

    [EventSubscription] // Both local and networked events
    private void OnDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg, EntitySessionEventArgs args)
    {
        var dragged = GetEntity(msg.DraggedEntityUid);

        if (!DraggableQuery.TryComp(dragged, out TabletopDraggableComponent? draggableComponent))
            return;

        draggableComponent.DraggingPlayer = msg.IsDragging ? args.SenderSession.UserId : null;
        Dirty(dragged, draggableComponent);

        if (!TryComp(dragged, out AppearanceComponent? appearance))
            return;

        if (draggableComponent.DraggingPlayer != null)
        {
            Appearance.SetData(dragged, TabletopItemVisuals.Scale, new Vector2(1.25f, 1.25f), appearance);
            Appearance.SetData(dragged, TabletopItemVisuals.DrawDepth, (int)DrawDepth.DrawDepth.Items + 1, appearance);
        }
        else
        {
            Appearance.SetData(dragged, TabletopItemVisuals.Scale, Vector2.One, appearance);
            Appearance.SetData(dragged, TabletopItemVisuals.DrawDepth, (int)DrawDepth.DrawDepth.Items, appearance);
        }
    }

    [SubscribeLocalEvent]
    private void OnInRangeOverride(Entity<TabletopGamerComponent> ent, ref InRangeOverrideEvent args)
    {
        if (args.Handled)
            return;

        if (!DraggableQuery.HasComp(args.Target) && !BackgroundQuery.HasComp(args.Target))
            return;

        // Assume that this can be dragged.
        args.InRange = true;
        args.Handled = true;
    }

    [Serializable, NetSerializable]
    public sealed class TabletopDraggableComponentState(NetUserId? draggingPlayer) : ComponentState
    {
        public NetUserId? DraggingPlayer = draggingPlayer;
    }
    #endregion Event Handlers

    #region Utility
    /// <summary>
    /// Whether the table exists, and the player can interact with it.
    /// </summary>
    /// <param name="playerEntity">The player entity to check.</param>
    /// <param name="table">The table entity to check.</param>
    protected bool CanSeeTable(EntityUid playerEntity, EntityUid? table)
    {
        // Table may have been deleted, hence TryComp.
        if (!TryComp(table, out MetaDataComponent? meta)
            || meta.EntityLifeStage >= EntityLifeStage.Terminating
            || (meta.Flags & MetaDataFlags.InContainer) == MetaDataFlags.InContainer)
        {
            return false;
        }

        return _interaction.InRangeUnobstructed(playerEntity, table.Value) && _actionBlocker.CanInteract(playerEntity, table);
    }

    protected bool CanDrag(EntityUid playerEntity, EntityUid target, [NotNullWhen(true)] out TabletopDraggableComponent? draggable)
    {
        if (!DraggableQuery.TryComp(target, out draggable))
            return false;

        // CanSeeTable checks interaction action blockers. So no need to check them here.
        // If this ever changes, so that ghosts can spectate games, then the check needs to be moved here.
        return TryComp(playerEntity, out HandsComponent? hands) && hands.Hands.Count > 0;
    }

    private void RemovePiece(EntityUid piece, Entity<TabletopGameComponent> table, EntityUid user)
    {
        if (!table.Comp.HasSession)
            return;

        // If this is the client, just assume it's valid
        if (_net.IsServer && !_userInterface.GetActors(table.Owner, TabletopGameUiKey.Key).Contains(user))
            return;

        if (table.Comp.Entities.Remove(piece))
        {
            PredictedQueueDel(piece);
            Dirty(table);
        }
    }
    #endregion
}
