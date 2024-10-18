using Content.Server.Administration.Logs;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Database;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Players;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Clothing.Systems;

/// <inheritdoc/>
public sealed class CursedMaskSystem : SharedCursedMaskSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    // We can't store this info on the component easily
    private static readonly ProtoId<HTNCompoundPrototype> TakeoverRootTask = "SimpleHostileCompound";

    protected override void TryTakeover(Entity<CursedMaskComponent> ent, EntityUid wearer)
    {
        if (ent.Comp.CurrentState != CursedMaskExpression.Anger)
            return;

        if (TryComp<ActorComponent>(wearer, out var actor) && actor.PlayerSession.GetMind() is { } mind)
        {
            var session = actor.PlayerSession;
            if (!_ghostSystem.OnGhostAttempt(mind, false))
                return;

            ent.Comp.StolenMind = mind;

            _popup.PopupEntity(Loc.GetString("cursed-mask-takeover-popup"), wearer, session, PopupType.LargeCaution);
            _adminLog.Add(LogType.Action,
                LogImpact.Extreme,
                $"{ToPrettyString(wearer):player} had their body taken over and turned into an enemy through the cursed mask {ToPrettyString(ent):entity}");
        }

        var npcFaction = EnsureComp<NpcFactionMemberComponent>(wearer);
        ent.Comp.OldFactions = npcFaction.Factions;
        _npcFaction.ClearFactions((wearer, npcFaction), false);
        _npcFaction.AddFaction((wearer, npcFaction), ent.Comp.CursedMaskFaction);

        ent.Comp.HasNpc = !EnsureComp<HTNComponent>(wearer, out var htn);
        htn.RootTask = new HTNCompoundTask { Task = TakeoverRootTask };
        htn.Blackboard.SetValue(NPCBlackboard.Owner, wearer);
        _npc.WakeNPC(wearer, htn);
        _htn.Replan(htn);
    }

    protected override void OnClothingUnequip(Entity<CursedMaskComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        // If we are taking off the cursed mask
        if (ent.Comp.CurrentState == CursedMaskExpression.Anger)
        {
            if (ent.Comp.HasNpc)
                RemComp<HTNComponent>(args.Wearer);

            var npcFaction = EnsureComp<NpcFactionMemberComponent>(args.Wearer);
            _npcFaction.RemoveFaction((args.Wearer, npcFaction), ent.Comp.CursedMaskFaction, false);
            _npcFaction.AddFactions((args.Wearer, npcFaction), ent.Comp.OldFactions);

            ent.Comp.HasNpc = false;
            ent.Comp.OldFactions.Clear();

            if (Exists(ent.Comp.StolenMind))
            {
                _mind.TransferTo(ent.Comp.StolenMind.Value, args.Wearer);
                _adminLog.Add(LogType.Action,
                    LogImpact.Extreme,
                    $"{ToPrettyString(args.Wearer):player} was restored to their body after the removal of {ToPrettyString(ent):entity}.");
                ent.Comp.StolenMind = null;
            }
        }

        RandomizeCursedMask(ent, args.Wearer);
    }
}
