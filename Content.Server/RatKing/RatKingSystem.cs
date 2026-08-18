using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Pointing;
using Content.Shared.Random.Helpers;
using Content.Shared.RatKing.Components;
using Content.Shared.RatKing.Events;
using Content.Shared.RatKing.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.RatKing;

/// <inheritdoc/>
public sealed partial class RatKingSystem : SharedRatKingSystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;

    /// <summary>
    /// Summons an allied rat servant at the King, costing a small amount of hunger.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRaiseArmy(Entity<RatKingComponent> ent, ref RatKingRaiseArmyActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SatiationComponent>(ent.Owner, out var satiation))
            return;

        if (_satiation.GetValueOrNull((ent.Owner, satiation), SatiationSystem.Hunger) < ent.Comp.HungerPerArmyUse)
        {
            _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), ent.Owner, ent.Owner);
            return;
        }

        args.Handled = true;
        _satiation.ModifyValue((ent.Owner, satiation), SatiationSystem.Hunger, -ent.Comp.HungerPerArmyUse);
        var servant = Spawn(ent.Comp.ArmyMobSpawnId, Transform(ent.Owner).Coordinates);
        var servantComp = EnsureComp<RatKingServantComponent>(servant);
        servantComp.King = ent.Owner;
        Dirty(servant, servantComp);

        ent.Comp.Servants.Add(servant);
        _npc.SetBlackboard(servant, NPCBlackboard.FollowTarget, new EntityCoordinates(ent.Owner, Vector2.Zero));
        UpdateServantNpc(servant, ent.Comp.CurrentOrder);
    }

    /// <summary>
    /// Uses hunger to release a specific amount of ammonia into the air.
    /// This heals the Rat King and his servants through a specific metabolism.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDomain(Entity<RatKingComponent> ent, ref RatKingDomainActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<SatiationComponent>(ent.Owner, out var satiation))
            return;

        if (_satiation.GetValueOrNull((ent.Owner, satiation), SatiationSystem.Hunger) < ent.Comp.HungerPerDomainUse)
        {
            _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), ent.Owner, ent.Owner);
            return;
        }

        args.Handled = true;
        _satiation.ModifyValue((ent.Owner, satiation), SatiationSystem.Hunger, -ent.Comp.HungerPerDomainUse);

        _popup.PopupEntity(Loc.GetString("rat-king-domain-popup"), ent.Owner);

        var tileMix = _atmos.GetTileMixture(ent.Owner, excite: true);
        tileMix?.AdjustMoles(Gas.Ammonia, ent.Comp.MolesAmmoniaPerDomain);
    }

    [SubscribeLocalEvent]
    private void OnPointedAt(Entity<RatKingComponent> ent, ref AfterPointedAtEvent args)
    {
        if (ent.Comp.CurrentOrder != RatKingOrderType.CheeseEm)
            return;

        foreach (var servant in ent.Comp.Servants)
        {
            _npc.SetBlackboard(servant, NPCBlackboard.CurrentOrderedTarget, args.Pointed);
        }
    }

    protected override void UpdateServantNpc(EntityUid uid, RatKingOrderType orderType)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
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
            return;

        var msg = _random.Pick(datasetPrototype);
        _chat.TrySendInGameICMessage(ent.Owner, msg, InGameICChatType.Speak, true);
    }
}
