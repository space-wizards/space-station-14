// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.BloodCult;
using Content.Server.BloodCult;
using Content.Server.BloodCult.EntitySystems;
using Content.Server.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Systems;
using Robust.Server.GameObjects;
using Content.Shared.BloodCult.Components;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// System that handles Blood Cult deconversion when a mindshield is implanted.
/// </summary>
public sealed class BloodCultMindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Subscribe to MindShieldComponent being added to avoid duplicate subscriptions with other systems
        SubscribeLocalEvent<MindShieldComponent, ComponentAdd>(OnMindShieldAdded);
    }

    /// <summary>
    /// Attempts to deconvert a blood cultist, stripping their abilities and restoring their faction alignment.
    /// </summary>
    public bool TryDeconvert(EntityUid uid, string? popupLocId = "cult-break-control", TimeSpan? stunDuration = null, bool log = true)
    {
        if (!HasComp<BloodCultistComponent>(uid))
            return false;

        var name = popupLocId != null ? Identity.Name(uid, EntityManager) : null;

        StripCultistAbilities(uid);

        if (_mindSystem.TryGetMind(uid, out var mindId, out _))
        {
            if (TryComp<MindComponent>(mindId, out var mindComp))
                _roleSystem.MindRemoveRole<BloodCultRoleComponent>((mindId, mindComp));
            _npcFaction.RemoveFaction(mindId, BloodCultRuleSystem.BloodCultistFactionId, false);
            // Possible to add other factions back here? It'd have to track their original faction
            // Todo: add a component to track original factions
            _npcFaction.AddFaction(mindId, BloodCultRuleSystem.NanotrasenFactionId);

            if (log)
            {
                _adminLogManager.Add(LogType.Mind, LogImpact.Medium,
                    $"{ToPrettyString(uid)} was deconverted from Blood Cult.");
            }
        }

        RemComp<BloodCultistComponent>(uid);

        var stunTime = stunDuration ?? TimeSpan.FromSeconds(4);
        if (stunTime > TimeSpan.Zero)
            _sharedStun.TryAddParalyzeDuration(uid, stunTime);

        if (popupLocId != null && name != null)
            _popupSystem.PopupEntity(Loc.GetString(popupLocId, ("name", name!)), uid);

        // Notify the player about memory loss
        _popupSystem.PopupEntity(Loc.GetString("cult-deconverted-memory-loss"), uid, uid, PopupType.Medium);

        return true;
    }

    public void StripCultistAbilities(EntityUid uid, bool removeVisuals = true)
    {
        // Remove actions from the entity's action component
        foreach (var actionId in _actions.GetActions(uid))
        {
            if (!TryComp<CultistSpellComponent>(actionId, out _))
                continue;

			_actions.RemoveAction((uid, null), (actionId, null));
        }

        // Also remove actions from the mind's action container if available
        if (_mindSystem.TryGetMind(uid, out var mindId, out _))
        {
            if (TryComp<ActionsContainerComponent>(mindId, out var containerComp))
            {
                foreach (var actionId in containerComp.Container.ContainedEntities.ToArray())
                {
                    if (!TryComp<CultistSpellComponent>(actionId, out _))
                        continue;

                    _actionContainer.RemoveAction((actionId, null));
                }
            }
        }

        if (!removeVisuals)
            return;

        // Remove the cultist visuals
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, CultEyesVisuals.CultEyes, false, appearance);
            _appearance.SetData(uid, CultHaloVisuals.CultHalo, false, appearance);
        }
    }

    /// <summary>
    /// When a mindshield component is added to an entity, check if they're a cultist and deconvert them.
    /// </summary>
    private void OnMindShieldAdded(EntityUid uid, MindShieldComponent comp, ComponentAdd args)
    {
        // Only process if they're a blood cultist
        if (!HasComp<BloodCultistComponent>(uid))
            return;

        TryDeconvert(uid);
    }
}

