using System.Diagnostics.CodeAnalysis;
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
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Tabletop;

public abstract partial class SharedTabletopSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transforms = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<TabletopDraggingPlayerChangedEvent>(OnDraggingPlayerChanged);

        SubscribeLocalEvent<TabletopGameComponent, GetVerbsEvent<ActivationVerb>>(AddPlayGameVerb);
        SubscribeLocalEvent<TabletopGameComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeNetworkEvent<TabletopRequestTakeOut>(OnTabletopRequestTakeOut);
    }

    private void OnTabletopRequestTakeOut(TabletopRequestTakeOut msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } playerSession)
            return;

        var table = GetEntity(msg.TableUid);

        if (!TryComp(table, out TabletopGameComponent? tabletop) || tabletop.Session is not { } session)
            return;

        if (!msg.Entity.IsValid())
            return;

        var entity = GetEntity(msg.Entity);

        if (!HasComp<TabletopHologramComponent>(entity))
        {
            _popup.PopupEntity(Loc.GetString("tabletop-error-remove-non-hologram"), table, args.SenderSession);
            return;
        }

        // Check if player is actually playing at this table.
        if (!session.Players.ContainsKey(playerSession))
            return;

        // Find the entity, remove it from the session and set it's position to the tabletop.
        session.Entities.TryGetValue(entity, out var result);
        session.Entities.Remove(result);
        PredictedQueueDel(result);
    }

    private void OnInteractUsing(Entity<TabletopGameComponent> ent, ref InteractUsingEvent args)
    {
        if (!_cfg.GetCVar(CCVars.GameTabletopPlace))
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        if (ent.Comp.Session is not { } session)
            return;

        if (!_hands.TryGetActiveItem(ent.Owner, out var handEnt))
            return;

        if (!HasComp<ItemComponent>(handEnt))
            return;

        var meta = MetaData(handEnt.Value);
        var protoId = meta.EntityPrototype?.ID;

        var hologram = EntityManager.PredictedSpawn(protoId, session.Position.Offset(-1, 0));

        // Make sure the entity can be dragged and can be removed, move it into the board game world and add it to the Entities hashmap.
        EnsureComp<TabletopDraggableComponent>(hologram);
        EnsureComp<TabletopHologramComponent>(hologram);
        session.Entities.Add(hologram);

        _popup.PopupClient(Loc.GetString("tabletop-added-piece"), ent.Owner, args.User);
    }

    /// <summary>
    /// Add a verb that allows the player to start playing a tabletop game.
    /// </summary>
    private void AddPlayGameVerb(Entity<TabletopGameComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var playVerb = new ActivationVerb()
        {
            Text = Loc.GetString("tabletop-verb-play-game"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
            Act = () => OpenSessionFor(actor.PlayerSession, ent.Owner)
        };

        args.Verbs.Add(playVerb);
    }

    /// <summary>
    /// Move an entity which is dragged by the user, but check if they are allowed to do so and to these coordinates.
    /// </summary>
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
        _transforms.SetParent(moved, transform, _map.GetMapOrInvalid(transform.MapID));
        _transforms.SetLocalPositionNoLerp(moved, msg.Coordinates.Position, transform);
    }

    private void OnDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg, EntitySessionEventArgs args)
    {
        var dragged = GetEntity(msg.DraggedEntityUid);

        if (!TryComp(dragged, out TabletopDraggableComponent? draggableComponent))
            return;

        draggableComponent.DraggingPlayer = msg.IsDragging ? args.SenderSession.UserId : null;
        Dirty(dragged, draggableComponent);

        if (!TryComp(dragged, out AppearanceComponent? appearance))
            return;

        if (draggableComponent.DraggingPlayer != null)
        {
            _appearance.SetData(dragged, TabletopItemVisuals.Scale, new Vector2(1.25f, 1.25f), appearance);
            _appearance.SetData(dragged, TabletopItemVisuals.DrawDepth, (int)DrawDepth.DrawDepth.Items + 1, appearance);
        }
        else
        {
            _appearance.SetData(dragged, TabletopItemVisuals.Scale, Vector2.One, appearance);
            _appearance.SetData(dragged, TabletopItemVisuals.DrawDepth, (int)DrawDepth.DrawDepth.Items, appearance);
        }
    }

    [Serializable, NetSerializable]
    public sealed class TabletopDraggableComponentState(NetUserId? draggingPlayer) : ComponentState
    {
        public NetUserId? DraggingPlayer = draggingPlayer;
    }

    [Serializable, NetSerializable]
    public sealed class TabletopRequestTakeOut : EntityEventArgs
    {
        public NetEntity Entity;
        public NetEntity TableUid;
    }

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
        if (!TryComp(target, out draggable))
            return false;

        // CanSeeTable checks interaction action blockers. So no need to check them here.
        // If this ever changes, so that ghosts can spectate games, then the check needs to be moved here.
        return TryComp(playerEntity, out HandsComponent? hands) && hands.Hands.Count > 0;
    }
    #endregion
}
