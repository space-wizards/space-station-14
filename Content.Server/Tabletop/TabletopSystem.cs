using JetBrains.Annotations;
using Content.Shared.Interaction;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Tabletop;

[UsedImplicitly]
public sealed partial class TabletopSystem : SharedTabletopSystem
{
    #region Overrides
    /// <inheritdoc />
    protected override void CopyEntity(EntityUid target, Entity<TabletopGameComponent> ent, EntityUid user)
    {
        if (ent.Comp.Position is not { } position)
            return;

        // Delay count check - prints should happen last.
        if (ent.Comp.Entities.Count >= MaxTabletopPieces)
        {
            Popup.PopupEntity(Loc.GetString("tabletop-cant-add-more"), ent, user);
            return;
        }

        var meta = MetaData(target);

        var hologram = EntityManager.PredictedSpawn(GamePiecePrototype, position.Offset(-1, 0));

        // Make sure the entity can be dragged and can be removed, move it into the board game world and add it to the Entities hashmap.
        EnsureComp<TabletopDraggableComponent>(hologram);
        EnsureComp<TabletopHologramComponent>(hologram);
        Meta.SetEntityName(hologram, Name(target, meta));

        // Try to get existing tabletop visuals if we can (copying existing pieces), otherwise get this entity's prototype of this object.
        if (AppearanceQuery.TryComp(target, out AppearanceComponent? appearance)
            && Appearance.TryGetData<string>(target, TabletopItemVisuals.Prototype, out var appearProto, appearance))
        {
            Appearance.SetData(hologram, TabletopItemVisuals.Prototype, appearProto);
        }
        else if (meta.EntityPrototype is { } metaProto)
        {
            Appearance.SetData(hologram, TabletopItemVisuals.Prototype, metaProto.ID);
        }

        ent.Comp.Entities.Add(hologram);
        Dirty(ent);

        Popup.PopupEntity(Loc.GetString("tabletop-added-piece"), ent, user);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TabletopGamerComponent>();
        while (query.MoveNext(out var uid, out var gamer))
        {
            if (!Exists(gamer.Tabletop))
                continue;

            if (!ActorQuery.TryComp(uid, out ActorComponent? actor))
            {
                RemCompDeferred<TabletopGamerComponent>(uid);
                continue;
            }

            if (actor.PlayerSession.Status != SessionStatus.InGame || !CanSeeTable(uid, gamer.Tabletop))
                CloseSessionFor(actor.PlayerSession, gamer.Tabletop);
        }
    }
    #endregion Overrides

    #region Network Handlers
    [SubscribeNetworkEvent]
    private void OnStopPlaying(TabletopStopPlayingEvent msg, EntitySessionEventArgs args)
    {
        CloseSessionFor(args.SenderSession, GetEntity(msg.TableUid));
    }
    #endregion Network Handlers

    #region Local Handlers
    protected override void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { } playerSession)
            return;

        if (!GameQuery.TryComp(GetEntity(msg.TableUid), out TabletopGameComponent? tabletop) || !tabletop.HasSession)
            return;

        // Check if player is actually playing at this table.
        if (!tabletop.Players.ContainsKey(playerSession))
            return;

        base.OnTabletopMove(msg, args);
    }

    [SubscribeLocalEvent]
    private void OnGameShutdown(Entity<TabletopGameComponent> ent, ref ComponentShutdown args)
    {
        CleanupSession(ent.Owner);
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
        if (!ActorQuery.TryComp(ent.Owner, out ActorComponent? actor))
            return;

        if (ent.Comp.Tabletop.IsValid())
            CloseSessionFor(actor.PlayerSession, ent.Comp.Tabletop);
    }
    #endregion Local Handlers
}
