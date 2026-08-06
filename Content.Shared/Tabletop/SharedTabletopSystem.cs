using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Tabletop;

/// <summary>
/// System driving the behavior of tabletop games.
/// Allows
/// </summary>
public abstract partial class SharedTabletopSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedViewSubscriberSystem _viewSubscriber = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery;
    [Dependency] private EntityQuery<TabletopBackgroundComponent> _backgroundQuery;
    [Dependency] protected EntityQuery<TabletopDraggableComponent> DraggableQuery;
    [Dependency] private EntityQuery<TabletopGameComponent> _gameQuery;
    [Dependency] private EntityQuery<TabletopGamerComponent> _gamerQuery;
    [Dependency] private EntityQuery<TabletopHologramComponent> _hologramQuery;
    [Dependency] private EntityQuery<UserInterfaceComponent> _uiQuery;

    /// <summary>
    /// The prototype to use to represent items dragged into the tabletop map.
    /// </summary>
    protected static readonly EntProtoId GamePiecePrototype = "BaseTabletopPiece";

    /// <summary>
    /// The maximum number of pieces to allow placement on a table.
    /// </summary>
    protected static readonly int MaxTabletopPieces = 50;

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
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_gamerQuery.TryComp(args.User, out var gamer)
            || !_gameQuery.TryComp(gamer.Tabletop, out var game))
            return;

        // Will get closed later if IsPlaying returns false.
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
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_gamerQuery.TryComp(args.User, out var gamer)
            || !_gameQuery.TryComp(gamer.Tabletop, out var game))
            return;

        // TODO: change this out for a UI check
        // Will get closed later if IsPlaying returns false.
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
    /// Move an entity which is dragged by the user, but check if they are allowed to do so and to these coordinates.
    /// </summary>
    [EventSubscription] // Both local and networked events
    protected virtual void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } playerSession || playerSession.AttachedEntity is not { } playerUid)
            return;

        var tableUid = GetEntity(msg.TableUid);

        if (!_gameQuery.TryComp(tableUid, out TabletopGameComponent? tabletop) || !tabletop.HasSession)
            return;

        // Check if player is actually playing at this table.
        if (!IsPlaying(playerUid, tableUid))
            return;

        var moved = GetEntity(msg.MovedEntityUid);

        if (!CanDrag(playerUid, moved, out _))
            return;

        // Move the entity and dirty it (should stay parented to the board it was created from)
        var transform = Comp<TransformComponent>(moved);
        _xform.SetLocalPosition(moved, msg.Position, transform);
    }

    [EventSubscription] // Both local and networked events
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
        return _ui.GetActors(table, TabletopGameUiKey.Key).Contains(playerEntity);
    }

    protected bool CanDrag(EntityUid playerEntity, EntityUid target, [NotNullWhen(true)] out TabletopDraggableComponent? draggable)
    {
        if (!DraggableQuery.TryComp(target, out draggable))
            return false;

        // We currently only check that the playing needs hands
        return TryComp(playerEntity, out HandsComponent? hands) && hands.Hands.Count > 0;
    }

    private void RemovePiece(EntityUid piece, Entity<TabletopGameComponent> table, EntityUid user)
    {
        if (!table.Comp.HasSession)
            return;

        // If this is the client, just assume it's valid
        if (!IsPlaying(user, table))
            return;

        // Only holograms are
        if (!_hologramQuery.TryComp(piece, out var hologram)
            || hologram.Table != table.Owner)
            return;

        _adminLog.Add(LogType.Action, $"{user:player} removed piece {ToPrettyString(piece)}, from board {ToPrettyString(table)}");

        _popup.PopupCoordinates(Loc.GetString("tabletop-removed-piece-on-board"), Transform(piece).Coordinates, PopupType.Medium);

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
        // Display a message above the piece telling anyone playing that it showed up.
        _popup.PopupEntity(Loc.GetString("tabletop-added-piece-on-board"), hologram, PopupType.Medium);
    }
    #endregion
}
