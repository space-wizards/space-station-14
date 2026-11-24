using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Pointing;
using Content.Shared.Random.Helpers;
using Content.Shared.RatKing.Components;
using Content.Shared.RatKing.Events;
using Content.Shared.RatKing.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.RatKing;

/// <inheritdoc/>
public sealed partial class ServerRatKingSystem : RatKingSystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private NPCPointCommandSystem _pointCommands = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<HTNComponent> _htnQuery;

    /// <summary>
    /// Summons an allied rat servant at the King's position.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRaiseArmy(Entity<RatKingComponent> ent, ref RatKingRaiseArmyActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var servant = SpawnAtPosition(ent.Comp.ArmyMobSpawnId, Transform(ent.Owner).Coordinates);
        var servantComp = EnsureComp<RatKingServantComponent>(servant);
        servantComp.King = ent.Owner;
        Dirty(servant, servantComp);

        ent.Comp.Servants.Add(servant);
        _npc.SetBlackboard(servant, NPCBlackboard.FollowTarget, new EntityCoordinates(ent.Owner, Vector2.Zero));
        UpdateServantNpc(servant, ent.Comp.CurrentOrder);
    }

    /// <summary>
    /// Releases ammonia into the air, healing the Rat King and his servants.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDomain(Entity<RatKingComponent> ent, ref RatKingDomainActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("rat-king-domain-popup"), ent.Owner);

        var tileMix = _atmos.GetTileMixture(ent.Owner, excite: true);
        tileMix?.AdjustMoles(Gas.Ammonia, ent.Comp.MolesAmmoniaPerDomain);
    }

    /// <summary>
    /// Commands the Rat King's servants to move to the pointed location.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPointedAt(Entity<RatKingComponent> ent, ref AfterPointedAtEvent args)
    {
        if (ent.Comp.CurrentOrder != RatKingOrderType.CheeseEm)
            return;

        _pointCommands.CommandFollowers(ent.Comp.Servants, args.Pointed);
    }

    protected override void UpdateServantNpc(EntityUid uid, RatKingOrderType orderType)
    {
        if (!_htnQuery.TryComp(uid, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, orderType);
        _htn.Replan(htn);
    }

    protected override void DoCommandCallout(Entity<RatKingComponent> ent)
    {
        if (!ent.Comp.OrderCallouts.TryGetValue(ent.Comp.CurrentOrder, out var datasetId) ||
            !ProtoMan.TryIndex(datasetId, out var datasetPrototype))
        {
            return;
        }

        var msg = _random.Pick(datasetPrototype);
        _chat.TrySendInGameICMessage(ent.Owner, msg, InGameICChatType.Speak, true);
    }
}
