using System.Linq;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Tabletop;

/// <summary>
/// System for simulating tabletop games.
/// Works using a dedicated map for board game boards.
/// All tabletop windows have views into this map, where pieces can be dragged about by anyone playing the game.
/// </summary>
public abstract partial class SharedTabletopSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] protected SharedUserInterfaceSystem UI = default!;
    [Dependency] private SharedViewSubscriberSystem _viewSubscriber = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery;
    [Dependency] private EntityQuery<ItemComponent> _itemQuery;
    [Dependency] private EntityQuery<TabletopBackgroundComponent> _backgroundQuery;
    [Dependency] protected EntityQuery<TabletopDraggableComponent> DraggableQuery;
    [Dependency] private EntityQuery<TabletopGameComponent> _gameQuery;
    [Dependency] private EntityQuery<TabletopGamerComponent> _gamerQuery;
    [Dependency] private EntityQuery<TabletopHologramComponent> _hologramQuery;

    /// <summary>
    /// The prototype to use to represent items dragged into the tabletop map.
    /// </summary>
    protected static readonly EntProtoId GamePiecePrototype = "BaseTabletopPiece";

    /// <summary>
    /// The maximum number of pieces to allow placement on a table.
    /// </summary>
    protected const int MaxTabletopPieces = 50;

    /// <summary>
    /// The number of pixels per meter, used to determine board bounds.
    /// </summary>
    /// <remarks>
    /// Yes this is disgusting but specifying "board size" off of a texture makes no sense in meters.
    /// </remarks>
    protected const float PixelsPerMeter = 32f;

    /// <summary>
    /// A handler for drag/drop handling, useful on the client.
    /// </summary>
    protected virtual void DragUpdated(Entity<TabletopDraggableComponent> ent) { }

    #region Event Handlers

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<TabletopGameComponent> ent, ref InteractUsingEvent args)
    {
        if (!_cfg.GetCVar(CCVars.GameTabletopPlace))
            return;

        if (ent.Comp.Board is null)
            return;

        if (!_hands.TryGetActiveItem(args.User, out var maybeHandEnt) || maybeHandEnt is not { } handEnt)
            return;

        if (!_itemQuery.HasComp(handEnt))
            return;

        CopyEntity(handEnt, ent, args.User);
    }

    [SubscribeLocalEvent]
    private void OnTabletopBoundUIOpened(Entity<TabletopGameComponent> ent, ref BoundUIOpenedEvent args)
    {
        // Check that a player is attached to the entity.
        if (!_actorQuery.TryComp(args.Actor, out ActorComponent? actor))
            return;

        OpenSessionFor(actor.PlayerSession, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnTabletopBoundUIClosed(Entity<TabletopGameComponent> ent, ref BoundUIClosedEvent args)
    {
        // Check that a player is attached to the entity.
        if (!_actorQuery.TryComp(args.Actor, out ActorComponent? actor))
            return;

        CloseSessionFor(actor.PlayerSession, ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnGameShutdown(Entity<TabletopGameComponent> ent, ref ComponentShutdown args)
    {
        TeardownBoard(ent.AsNullable());
    }

    [SubscribeLocalEvent]
    private void AddDraggableCopyVerb(Entity<TabletopDraggableComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        // Is this a piece from a board game that we can interact with?
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_gamerQuery.TryComp(args.User, out var gamer)
            || !_gameQuery.TryComp(gamer.Tabletop, out var game))
            return;

        // A user has to be playing a game to drag a piece.
        var disabled = !IsPlaying(args.User, gamer.Tabletop);
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
        // Is this a piece from a board game that we can interact with?
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_gamerQuery.TryComp(args.User, out var gamer)
            || !_gameQuery.TryComp(gamer.Tabletop, out var game))
            return;

        // A user has to be playing a game to remove a piece.
        var disabled = !IsPlaying(args.User, gamer.Tabletop);
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
    /// Move an entity which is dragged by the user,
    /// first checking if they're allowed to,
    /// and clamping the coordinates to the board.
    /// </summary>
    [EventSubscription] // Both local events (for clients) and networked events (for the server)
    protected virtual void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } playerSession || playerSession.AttachedEntity is not { } playerUid)
            return;

        var tableUid = GetEntity(msg.TableUid);

        if (!_gameQuery.TryComp(tableUid, out TabletopGameComponent? tabletop) || tabletop.Board is null)
            return;

        // Check if player is actually playing at this table.
        if (!IsPlaying(playerUid, tableUid))
            return;

        var moved = GetEntity(msg.MovedEntityUid);

        if (!DraggableQuery.HasComp(moved))
            return;

        // Move the entity, making sure to keep it on the board!
        var transform = Transform(moved);
        var bounds = tabletop.Size / (2 * PixelsPerMeter);
        var clampedPosition = Vector2.Clamp(msg.Position, -bounds, bounds);
        _xform.SetLocalPosition(moved, clampedPosition, transform);
    }

    [EventSubscription] // Both local events (for clients) and networked events (for the server)
    private void OnDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg, EntitySessionEventArgs args)
    {
        var dragged = GetEntity(msg.DraggedEntityUid);

        if (!DraggableQuery.TryComp(dragged, out TabletopDraggableComponent? draggable))
            return;

        draggable.DraggingPlayer = msg.IsDragging ? args.SenderSession.UserId : null;
        Dirty(dragged, draggable);

        DragUpdated((dragged, draggable));
    }

    [SubscribeLocalEvent]
    private void OnInRangeOverride(Entity<TabletopGamerComponent> ent, ref InRangeOverrideEvent args)
    {
        if (args.Handled)
            return;

        if (!DraggableQuery.HasComp(args.Target)
            && !_backgroundQuery.HasComp(args.Target))
            return;

        // Assume that this can be dragged.
        args.InRange = true;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<TabletopGamerComponent> ent, ref PlayerDetachedEvent args)
    {
        if (ent.Comp.Tabletop.IsValid())
            CloseSessionFor(args.Player, ent.Comp.Tabletop);
    }

    [SubscribeLocalEvent]
    private void OnGamerShutdown(Entity<TabletopGamerComponent> ent, ref ComponentShutdown args)
    {
        if (!_actorQuery.TryComp(ent.Owner, out ActorComponent? actor))
            return;

        if (ent.Comp.Tabletop.IsValid())
            CloseSessionFor(actor.PlayerSession, ent.Comp.Tabletop);
    }
    #endregion Event Handlers

    #region Utility
    /// <summary>
    /// Checks and returns whether <paramref name="playerEntity"/> is playing on <paramref name="table"/>.
    /// </summary>
    protected bool IsPlaying(EntityUid playerEntity, EntityUid table)
    {
        return UI.GetActors(table, TabletopGameUiKey.Key).Contains(playerEntity);
    }

    private void RemovePiece(EntityUid piece, Entity<TabletopGameComponent> table, EntityUid user)
    {
        if (table.Comp.Board is null)
            return;

        // If this is the client, just assume it's valid
        if (!IsPlaying(user, table))
            return;

        // Only holograms are
        if (!_hologramQuery.TryComp(piece, out var hologram)
            || hologram.Table != table.Owner)
            return;

        _adminLog.Add(LogType.Action, $"{user:player} removed piece {ToPrettyString(piece)}, from board {ToPrettyString(table)}");

        PredictedQueueDel(piece);
    }

    /// <summary>
    /// Creates a sanitized copy of an entity and sends it into a particular tabletop game.
    /// </summary>
    /// <param name="target">The entity to copy.</param>
    /// <param name="ent">The tabletop game to send the piece into.</param>
    /// <param name="user">The user to show a popup on </param>
    protected void CopyEntity(EntityUid target, Entity<TabletopGameComponent> ent, EntityUid user)
    {
        if (ent.Comp.Board is not { } board)
            return;

        var boardXform = Transform(board);

        // Delay count check - prints should happen last.
        if (boardXform.ChildCount >= MaxTabletopPieces)
        {
            _popup.PopupEntity(Loc.GetString("tabletop-cant-add-more"), ent, user);
            return;
        }

        var meta = MetaData(target);

        var hologram = PredictedSpawnAttachedTo(GamePiecePrototype, new(board, ent.Comp.SpawnOffset));

        // Make sure the entity can be dragged and removed, move it into the board game world and add it to the Entities hashmap.
        EnsureComp<TabletopDraggableComponent>(hologram);

        var hologramComp = EnsureComp<TabletopHologramComponent>(hologram);
        hologramComp.Table = ent;
        Dirty(hologram, hologramComp);

        _meta.SetEntityName(hologram, Name(target, meta));

        // Try to get existing tabletop visuals if we can (copying existing pieces), otherwise get this entity's prototype from its metadata.
        if (_appearanceQuery.TryComp(target, out AppearanceComponent? appearance)
            && Appearance.TryGetData<string>(target, TabletopItemVisuals.Prototype, out var appearProto, appearance))
        {
            Appearance.SetData(hologram, TabletopItemVisuals.Prototype, appearProto);
        }
        else if (meta.EntityPrototype is { } metaProto)
        {
            Appearance.SetData(hologram, TabletopItemVisuals.Prototype, metaProto.ID);
        }

        _adminLog.Add(LogType.Action, $"{user:player} created piece {ToPrettyString(hologram)}, copying {target:subject} onto board {ToPrettyString(ent)}");

        // Display a message to the user telling them the piece was added.
        _popup.PopupEntity(Loc.GetString("tabletop-added-piece"), ent, user);
    }
    #endregion
}
