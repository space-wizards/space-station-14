// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.DeadSpace.Spiders.SpiderInfector;
using Content.Shared.DeadSpace.Spiders.SpiderInfector.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectorDead;
using Content.Shared.Body.Components;
using Content.Server.Popups;
using Content.Shared.Zombies;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Server.Zombies;
using Content.Shared.Actions;
using Content.Server.DeadSpace.Spiders.SpiderTerror.Components;
using Content.Shared.DeadSpace.Abilities.Egg.Components;
using Content.Shared.DeadSpace.Abilities.Egg;

namespace Content.Server.DeadSpace.InfectorDead.EntitySystems;

public sealed partial class SpiderInfectorSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedEggSystem _eggSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderInfectorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SpiderInfectorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpiderInfectorComponent, SpiderInfectorActionEvent>(OnInfect);
        SubscribeLocalEvent<SpiderInfectorComponent, InfectorDeadDoAfterEvent>(OnDoAfter);
    }

    private void OnComponentInit(EntityUid uid, SpiderInfectorComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.SpiderInfectorActionEntity, component.SpiderInfectorAction, uid);
    }

    private void OnShutdown(EntityUid uid, SpiderInfectorComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.SpiderInfectorActionEntity);
    }
    private void OnInfect(EntityUid uid, SpiderInfectorComponent component, SpiderInfectorActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        var target = args.Target;

        if (!IsInfectionPossible(uid, target))
            return;

        BeginInfected(uid, target, component);

        args.Handled = true;
    }

    private void BeginInfected(EntityUid uid, EntityUid target, SpiderInfectorComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, component.InfectedDuration, new InfectorDeadDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 2
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, SpiderInfectorComponent component, InfectorDeadDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!IsInfectionPossible(uid, args.Args.Target.Value))
            return;

        var eggComponent = EntityManager.AddComponent<EggComponent>(args.Args.Target.Value);

        eggComponent.SpawnedEntities = component.SpawnedEntities;
        _eggSystem.Postpone(component.InfectDuration, eggComponent);

        args.Handled = true;
    }

    public bool IsInfectionPossible(EntityUid uid, EntityUid target)
    {
        if (_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель не должна быть мертва!"), uid, uid);
            return false;
        }

        if (!HasComp<BodyComponent>(target))
            return false;

        if (HasComp<EggComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель уже заражена!"), uid, uid);
            return false;
        }

        if (HasComp<SpiderTerrorComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель вашь сородич!"), uid, uid);
            return false;
        }

        if (HasComp<InfectionDeadComponent>(target) || HasComp<NecromorfComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель чем-то больна?"), uid, uid);
            return false;
        }

        if (HasComp<ZombieComponent>(target) || HasComp<PendingZombieComponent>(target) || HasComp<ZombifyOnDeathComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель чем-то больна?"), uid, uid);
            return false;
        };

        if (!_eggSystem.IsInfectPossible(target))
            return false;

        return true;
    }
}
