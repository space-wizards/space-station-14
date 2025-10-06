// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Renegade.Components;
using Content.Shared.Stunnable;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.DeadSpace.Renegade.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using System.Linq;
using Content.Shared.Tag;
using Content.Shared.Implants.Components;
using Content.Shared.Implants;

namespace Content.Server.DeadSpace.Renegade;

public sealed class RenegadeSubordinateSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly RenegadeSubmissionConditionSystem _RenegadeSubmissionConditionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _sharedSubdermalImplantSystem = default!;

    private EntityQuery<RenegadeSubmissionConditionComponent> _objQuery;

    public override void Initialize()
    {
        base.Initialize();

        _objQuery = GetEntityQuery<RenegadeSubmissionConditionComponent>();

        SubscribeLocalEvent<RenegadeSubordinateComponent, MobStateChangedEvent>(OnState);
        SubscribeLocalEvent<RenegadeSubordinateComponent, ComponentStartup>(OnComponentStartUp);
        SubscribeLocalEvent<RenegadeSubordinateComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<MindShieldComponent, ComponentStartup>(MindShieldImplanted);
    }

    private void OnState(EntityUid uid, RenegadeSubordinateComponent component, MobStateChangedEvent args)
    {
        if (!TryGetMasterMind(uid, component, out var mind))
            return;

        if (mind == null)
            return;

        foreach (var objId in mind.Objectives)
        {
            if (_objQuery.TryGetComponent(objId, out var objComp))
            {
                bool contains = objComp.SubordinateCommand.Any(e => e.Equals(uid));

                if (_mobState.IsDead(uid) && contains)
                {
                    component.IsSubordinate = false;
                    _RenegadeSubmissionConditionSystem.TryResetSubordination(objId, uid, objComp);
                }

                if (!_mobState.IsDead(uid) && !component.IsSubordinate)
                {
                    _RenegadeSubmissionConditionSystem.SubordinationOfCommandCharged(objId, uid, objComp);
                    component.IsSubordinate = true;
                }
            }
        }
    }

    private void OnComponentStartUp(EntityUid uid, RenegadeSubordinateComponent component, ComponentStartup args)
    {
        if (TryComp<ImplantedComponent>(uid, out var implantedComp))
        {
            var implantContainer = implantedComp.ImplantContainer;
            foreach (var implantEntity in implantContainer.ContainedEntities)
            {
                if (HasComp<MindShieldImplantComponent>(implantEntity))
                {
                    _sharedSubdermalImplantSystem.ForceRemove(uid, implantEntity);
                    break;
                }
            }
        }

        if (HasComp<MindShieldComponent>(uid))
            RemComp<MindShieldComponent>(uid);

        if (!TryGetMasterMind(uid, component, out var mind))
            return;

        if (mind == null)
            return;

        foreach (var objId in mind.Objectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                _RenegadeSubmissionConditionSystem.SubordinationOfCommandCharged(objId, uid, obj);
                break;
            }
        }
    }

    private void OnComponentShutdown(EntityUid uid, RenegadeSubordinateComponent component, ComponentShutdown args)
    {
        LossObjectives(uid, component);
    }

    public void LossObjectives(EntityUid uid, RenegadeSubordinateComponent component)
    {
        if (!TryGetMasterMind(uid, component, out var mind))
        {
            if (TryComp<RenegadeSubmissionAbilityComponent>(component.Master, out var RenegadeSubmissionAbilityComp))
            {
                RenegadeSubmissionAbilityComp.Submissions -= 1;
            }
            return;
        }

        if (mind == null)
            return;

        foreach (var objId in mind.Objectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                _RenegadeSubmissionConditionSystem.TryResetSubordination(objId, uid, obj);
                break;
            }
        }
    }

    private void MindShieldImplanted(EntityUid uid, MindShieldComponent comp, ComponentStartup args)
    {
        if (HasComp<RenegadeSubordinateComponent>(uid))
        {
            var stunTime = TimeSpan.FromSeconds(10);
            var name = Identity.Entity(uid, EntityManager);
            RemComp<RenegadeSubordinateComponent>(uid);
            _sharedStun.TryUpdateParalyzeDuration(uid, stunTime);
            _popup.PopupEntity(Loc.GetString("Renegade-break-control", ("name", name)), uid);
        }
    }

    private bool TryGetMasterMind(EntityUid uid, RenegadeSubordinateComponent component, out MindComponent? mind)
    {
        mind = default;

        if (!HasComp<CommandStaffComponent>(uid))
            return false;

        if (!TryComp<MindContainerComponent>(component.Master, out var mindContainer) || !mindContainer.HasMind)
            return false;

        mind = Comp<MindComponent>(mindContainer.Mind.Value);
        return true;
    }
}
