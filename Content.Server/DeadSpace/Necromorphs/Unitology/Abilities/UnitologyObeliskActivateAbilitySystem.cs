// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Popups;
using Content.Shared.Actions;
using Robust.Shared.Containers;
using Content.Shared.Mobs.Systems;
using Content.Shared.DoAfter;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Server.DeadSpace.Necromorphs.InfectionDead;
using Content.Server.DeadSpace.Necromorphs.Necroobelisk.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Zombies;
using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;
using Content.Server.DeadSpace.Necromorphs.Necroobelisk;
using Content.Server.Chat.Systems;

namespace Content.Server.DeadSpace.Necromorphs.Unitology.Abilities;

public sealed class UnitologyObeliskSpawnAbilitySystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly NecromorfSystem _necromorfSystem = default!;
    [Dependency] private readonly InfectionDeadSystem _infectionDead = default!;
    [Dependency] private readonly NecroobeliskSystem _necroobelisk = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UnitologyObeliskActivateAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<UnitologyObeliskActivateAbilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<UnitologyObeliskActivateAbilityComponent, ObeliskActionEvent>(OnObeliskAction);
        SubscribeLocalEvent<UnitologyObeliskActivateAbilityComponent, ObeliskActivateDoAfterEvent>(OnDoAfter);
    }

    private void OnComponentInit(EntityUid uid, UnitologyObeliskActivateAbilityComponent comp, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref comp.ObeliskActionEntity, comp.ObeliskAction, uid);
    }

    private void OnShutdown(EntityUid uid, UnitologyObeliskActivateAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ObeliskActionEntity);
    }

    private void OnObeliskAction(EntityUid uid, UnitologyObeliskActivateAbilityComponent comp, ObeliskActionEvent args)
    {
        if (args.Handled)
            return;

        if (_container.IsEntityOrParentInContainer(uid))
            return;

        int countUnis = 0;
        bool hasObelisk = false;
        bool hasUni = false;
        bool hasSplinter = false;
        EntityUid? splinterUid = new EntityUid();
        var victims = _lookup.GetEntitiesInRange(uid, 4f);

        foreach (var victinUID in victims)
        {
            if (!hasObelisk)
            {
                if (EntityManager.HasComponent<NecroobeliskComponent>(victinUID))
                {
                    hasObelisk = true;
                    comp.Obelisk = victinUID;
                }
            }
            if (!hasSplinter)
            {
                if (EntityManager.HasComponent<NecroobeliskSplinterComponent>(victinUID))
                {
                    if (!_container.IsEntityOrParentInContainer(victinUID))
                    {
                        splinterUid = victinUID;
                        hasSplinter = true;
                    }
                }
            }
            if (!hasUni)
            {
                if (EntityManager.HasComponent<UnitologyComponent>(victinUID))
                {
                    if (_mobState.IsAlive(victinUID))
                    {
                        countUnis += 1;
                        _chat.TrySendInGameICMessage(victinUID, comp.IcMessage, InGameICChatType.Speak, true);
                        if (countUnis >= comp.CountUni)
                        {
                            hasUni = true;
                        }
                    }
                }
            }
            if (hasUni && hasSplinter)
            {
                BeginActivate(uid, splinterUid.Value, comp);
                args.Handled = true;
                return;
            }
        }

        _popupSystem.PopupEntity(Loc.GetString("Вы должны собрать команду из " + comp.CountUni + " юнитологов, положить осколок обелиска."), uid, uid);
        args.Handled = true;
    }

    private void BeginActivate(EntityUid uid, EntityUid target, UnitologyObeliskActivateAbilityComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, component.ObeliskActivateDuration, new ObeliskActivateDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 3,
            BreakOnDamage = true,
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, UnitologyObeliskActivateAbilityComponent component, ObeliskActivateDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null || component.Obelisk == null)
            return;

        QueueDel(args.Target);

        var obelisk = component.Obelisk.Value;

        if (!TryComp<NecroobeliskComponent>(obelisk, out var necroobeliskComp))
            return;

        _necroobelisk.SetActive(obelisk, true);

        _actionsSystem.RemoveAction(uid, component.ObeliskActionEntity);

        var query = EntityQueryEnumerator<UnitologyHeadComponent>();
        var queryUni = EntityQueryEnumerator<UnitologyComponent>();
        var queryEnsl = EntityQueryEnumerator<UnitologyEnslavedComponent>();

        while (query.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            _necromorfSystem.Necrofication(uniUid, component.AfterGibNecroPrototype);
        }

        while (queryUni.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            var necromorf = _infectionDead.GetRandomNecromorfPrototypeId();

            _necromorfSystem.Necrofication(uniUid, necromorf);
        }

        while (queryEnsl.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            var necromorf = _infectionDead.GetRandomNecromorfPrototypeId();

            _necromorfSystem.Necrofication(uniUid, necromorf);
        }

        var stageObeliskEvent = new StageObeliskEvent(obelisk);
        var ruleQuery = AllEntityQuery<UnitologyRuleComponent>();
        while (ruleQuery.MoveNext(out var ruleUid, out _))
        {
            RaiseLocalEvent(ruleUid, ref stageObeliskEvent);
        }
    }

}
