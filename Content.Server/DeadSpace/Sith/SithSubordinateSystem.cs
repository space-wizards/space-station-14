// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Sith.Components;
using Content.Shared.Stunnable;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.DeadSpace.Sith.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using System.Linq;
using Content.Shared.Tag;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Content.Shared.Implants;

namespace Content.Server.DeadSpace.Sith;

public sealed class SithSubordinateSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly SithSubmissionConditionsSystem _sithSubmissionConditionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _sharedSubdermalImplantSystem = default!;
    
    private EntityQuery<SithSubmissionConditionsComponent> _objQuery;

    [ValidatePrototypeId<TagPrototype>]
    public const string MindShieldTag = "MindShield";

    public override void Initialize()
    {
        base.Initialize();

        _objQuery = GetEntityQuery<SithSubmissionConditionsComponent>();

        SubscribeLocalEvent<SithSubordinateComponent, MobStateChangedEvent>(OnState);
        SubscribeLocalEvent<SithSubordinateComponent, ComponentStartup>(OnComponentStartUp);
        SubscribeLocalEvent<SithSubordinateComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<MindShieldComponent, ComponentStartup>(MindShieldImplanted);
    }

    private void OnState(EntityUid uid, SithSubordinateComponent component, MobStateChangedEvent args)
    {
        if (!TryGetMasterMind(uid, component, out var mind))
            return;

        if (mind == null)
            return;

        foreach (var objId in mind.AllObjectives)
        {
            if (_objQuery.TryGetComponent(objId, out var objComp))
            {
                bool contains = objComp.SubordinateCommand.Any(e => e.Equals(uid));

                if (_mobState.IsDead(uid) && contains)
                {
                    component.IsSubordinate = false;
                    _sithSubmissionConditionsSystem.TryResetSubordination(objId, uid, objComp);
                }

                if (!_mobState.IsDead(uid) && !component.IsSubordinate)
                {
                    _sithSubmissionConditionsSystem.SubordinationOfCommandCharged(objId, uid, objComp);
                    component.IsSubordinate = true;
                }
            }
        }
    }

    private void OnComponentStartUp(EntityUid uid, SithSubordinateComponent component, ComponentStartup args)
    {
        if (TryComp<ImplantedComponent>(uid, out var implantedComp))
        {
            var implantContainer = implantedComp.ImplantContainer;
            foreach (var implantEntity in implantContainer.ContainedEntities)
            {
                if (_tag.HasTag(implantEntity, MindShieldTag))
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

        foreach (var objId in mind.AllObjectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                _sithSubmissionConditionsSystem.SubordinationOfCommandCharged(objId, uid, obj);
                break;
            }
        }
    }

    private void OnComponentShutdown(EntityUid uid, SithSubordinateComponent component, ComponentShutdown args)
    {
        LossObjectives(uid, component);
    }

    public void LossObjectives(EntityUid uid, SithSubordinateComponent component)
    {
        if (!TryGetMasterMind(uid, component, out var mind))
        {
            if (TryComp<SithSubmissionAbilityComponent>(component.Master, out var sithSubmissionAbilityComp))
            {
                sithSubmissionAbilityComp.Submissions -= 1;
            }
            return;
        }

        if (mind == null)
            return;

        foreach (var objId in mind.AllObjectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                _sithSubmissionConditionsSystem.TryResetSubordination(objId, uid, obj);
                break;
            }
        }
    }

    private void MindShieldImplanted(EntityUid uid, MindShieldComponent comp, ComponentStartup args)
    {
        if (HasComp<SithSubordinateComponent>(uid))
        {
            var stunTime = TimeSpan.FromSeconds(10);
            var name = Identity.Entity(uid, EntityManager);
            RemComp<SithSubordinateComponent>(uid);
            _sharedStun.TryParalyze(uid, stunTime, true);
            _popup.PopupEntity(Loc.GetString("sith-break-control", ("name", name)), uid);
        }
    }

    private bool TryGetMasterMind(EntityUid uid, SithSubordinateComponent component, out MindComponent? mind)
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
