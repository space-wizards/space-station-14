using Content.Shared.Abilities.MinionMaster;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Pointing;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Abilities.MinionMaster;

/// <inheritdoc/>
public sealed class MinionMasterSystem : SharedMinionMasterSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MinionMasterComponent, MinionMasterRaiseArmyActionEvent>(OnRaiseArmy);
        SubscribeLocalEvent<MinionMasterComponent, AfterPointedAtEvent>(OnPointedAt);
    }

    /// <summary>
    /// Summons an allied minion to the minion master.
    /// </summary>
    private void OnRaiseArmy(EntityUid uid, MinionMasterComponent component, MinionMasterRaiseArmyActionEvent args)
    {
        if (args.Handled)
            return;

        //Do not bother checking for hunger if the MinionMasterComp does not require hunger to summon.
        if (component.DoesSummonCostFood)
        {
            //Fail to summon if hunger is required but there is no hunger component.
            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            else
            {
                //Fail to summon if hunger would go into the negatives.
                if (hunger.CurrentHunger < component.HungerPerArmyUse)
                {
                    _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, uid);
                    return;
                }

                _hunger.ModifyHunger(uid, -component.HungerPerArmyUse, hunger);
            }
        }

        args.Handled = true;

        var minion = Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates);
        var comp = EnsureComp<MinionComponent>(minion);
        comp.Master = uid;
        Dirty(minion, comp);

        component.Minions.Add(minion);
        _npc.SetBlackboard(minion, NPCBlackboard.FollowTarget, new EntityCoordinates(uid, Vector2.Zero));
        UpdateMinionNpc(minion, component.CurrentOrder);
    }

    private void OnPointedAt(EntityUid uid, MinionMasterComponent component, ref AfterPointedAtEvent args)
    {
        if (component.CurrentOrder != MinionOrderType.Attack)
            return;

        foreach (var minion in component.Minions)
        {
            _npc.SetBlackboard(minion, NPCBlackboard.CurrentOrderedTarget, args.Pointed);
        }
    }

    public override void UpdateMinionNpc(EntityUid uid, MinionOrderType orderType)
    {
        base.UpdateMinionNpc(uid, orderType);

        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, orderType);
        _htn.Replan(htn);
    }

    public override void DoCommandCallout(EntityUid uid, MinionMasterComponent component)
    {
        base.DoCommandCallout(uid, component);

        if (!component.OrderCallouts.TryGetValue(component.CurrentOrder, out var datasetId) ||
            !PrototypeManager.TryIndex<LocalizedDatasetPrototype>(datasetId, out var locDatasetPrototype))
            return;

        var msg = Random.Pick(locDatasetPrototype.Values);
        msg = _loc.GetString(msg);

        _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true);
    }
}
