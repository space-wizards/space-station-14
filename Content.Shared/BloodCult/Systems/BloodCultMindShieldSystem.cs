using System.Linq;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.BloodCult.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Content.Shared.BloodCult;

namespace Content.Shared.BloodCult.Systems;

/// <summary>
/// Handles Blood Cult deconversion when a mindshield is implanted. State change actions of abilities stripped, component removed, stun
/// Server-only side effects (admin logging) are handled by a server system subscribing to BloodCultDeconvertedEvent
/// </summary>
public sealed class BloodCultMindShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindShieldComponent, ComponentAdd>(OnMindShieldAdded);
    }

    /// <summary>
    /// Attempts to deconvert a blood cultist, stripping their abilities and restoring faction alignment.
    /// </summary>
    /// <param name="log">If true, a <see cref="BloodCultDeconvertedEvent"/> is raised so the server can log the deconversion.</param>
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
            _npcFaction.RemoveFaction((mindId, null), BloodCultConstants.BloodCultistFactionId, false);
            _npcFaction.AddFaction((mindId, null), BloodCultConstants.DefaultDeconversionFaction);
        }

        RemComp<BloodCultistComponent>(uid);

        var stunTime = stunDuration ?? TimeSpan.FromSeconds(4);
        if (stunTime > TimeSpan.Zero)
            _sharedStun.TryAddParalyzeDuration(uid, stunTime);

        if (popupLocId != null && name != null)
            _popupSystem.PopupEntity(Loc.GetString(popupLocId, ("name", name!)), uid);

        _popupSystem.PopupEntity(Loc.GetString("cult-deconverted-memory-loss"), uid, uid, PopupType.Medium);

        if (log)
            RaiseLocalEvent(new BloodCultDeconvertedEvent(uid));

        return true;
    }

    public void StripCultistAbilities(EntityUid uid, bool removeVisuals = true)
    {
        foreach (var actionId in _actions.GetActions(uid))
        {
            if (!TryComp<CultistSpellComponent>(actionId, out _))
                continue;

            _actions.RemoveAction((uid, null), (actionId, null));
        }

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

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, CultEyesVisuals.CultEyes, false, appearance);
            _appearance.SetData(uid, CultHaloVisuals.CultHalo, false, appearance);
        }
    }

    private void OnMindShieldAdded(Entity<MindShieldComponent> ent, ref ComponentAdd args)
    {
        if (!HasComp<BloodCultistComponent>(ent))
            return;

        TryDeconvert(ent);
    }
}
