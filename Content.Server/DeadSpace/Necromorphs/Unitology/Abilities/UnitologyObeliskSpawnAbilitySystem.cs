// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Popups;
using Content.Shared.Actions;
using Robust.Shared.Containers;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.Inventory;
using Content.Server.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Mobs.Components;
using Content.Server.Antag;
using Content.Server.DeadSpace.Necromorphs.Necroobelisk.Components;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Zombies;

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
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UnitologyObeliskSpawnAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<UnitologyObeliskSpawnAbilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<UnitologyObeliskSpawnAbilityComponent, ObeliskActionEvent>(OnObeliskAction);
        SubscribeLocalEvent<UnitologyObeliskSpawnAbilityComponent, ObeliskSpawnDoAfterEvent>(OnDoAfter);
    }

    private void OnComponentInit(EntityUid uid, UnitologyObeliskSpawnAbilityComponent comp, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref comp.ObeliskActionEntity, comp.ObeliskAction, uid);
    }

    private void OnShutdown(EntityUid uid, UnitologyObeliskSpawnAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ObeliskActionEntity);
    }

    private void OnObeliskAction(EntityUid uid, UnitologyObeliskSpawnAbilityComponent comp, ObeliskActionEvent args)
    {
        if (args.Handled)
            return;

        if (_container.IsEntityOrParentInContainer(uid))
            return;

        int countСorpses = 0;
        int countEnslaves = 0;
        bool hasСorpses = false;
        bool hasEnslaves = false;
        bool hasSplinter = false;
        EntityUid? splinterUid = new EntityUid();
        var victims = _lookup.GetEntitiesInRange(uid, 4f);

        foreach (var victinUID in victims)
        {
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
            if (!hasСorpses)
            {
                if (EntityManager.HasComponent<HumanoidAppearanceComponent>(victinUID))
                {
                    if (_mobState.IsDead(victinUID))
                    {
                        countСorpses += 1;
                        comp.VictimsUidList.Add(victinUID);

                        if (countСorpses >= comp.CountVictims)
                        {
                            hasСorpses = true;
                        }
                    }
                }
            }
            if (!hasEnslaves)
            {
                if (EntityManager.HasComponent<UnitologyEnslavedComponent>(victinUID))
                {
                    if (_mobState.IsAlive(victinUID))
                    {
                        countEnslaves += 1;

                        if (countEnslaves >= comp.CountEnslaves)
                        {
                            hasEnslaves = true;
                        }
                    }
                }
            }
            if (hasEnslaves && hasСorpses && hasSplinter)
            {
                BeginSpawn(uid, splinterUid.Value, comp);
                args.Handled = true;
                return;
            }
        }

        _popupSystem.PopupEntity(Loc.GetString("Вы должны принести в жертву " + comp.CountVictims + ", поработить трёх гуманоидов." + comp.CountEnslaves + ", положить осколок обелиска."), uid, uid);
        args.Handled = true;
    }

    private void BeginSpawn(EntityUid uid, EntityUid target, UnitologyObeliskSpawnAbilityComponent component)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, component.ObeliskSpawnDuration, new ObeliskSpawnDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 3,
            BreakOnMove = true
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;
    }

    private void OnDoAfter(EntityUid uid, UnitologyObeliskSpawnAbilityComponent component, ObeliskSpawnDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        QueueDel(args.Target);

        foreach (var entityUid in component.VictimsUidList)
        {
            QueueDel(entityUid);
        }

        var obelisk = Spawn(component.ObeliskPrototype, Transform(args.Args.Target.Value).Coordinates);

        _actionsSystem.RemoveAction(uid, component.ObeliskActionEntity);

        var query = EntityQueryEnumerator<UnitologyComponent>();
        var queryEnslave = EntityQueryEnumerator<UnitologyEnslavedComponent>();

        while (query.MoveNext(out var uniUid, out var comp))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid) || HasComp<UnitologyEnslavedComponent>(uniUid))
                continue;

            _necromorfSystem.Necrofication(uniUid, component.AfterGibNecroPrototype);
        }

        while (queryEnslave.MoveNext(out var uniUid, out var comp))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            _necromorfSystem.Necrofication(uniUid, component.AfterGibEnslavedNecroPrototype);
        }

        var stageObeliskEvent = new StageObeliskEvent(obelisk);
        var ruleQuery = AllEntityQuery<UnitologyRuleComponent>();
        while (ruleQuery.MoveNext(out var ruleUid, out _))
        {
            RaiseLocalEvent(ruleUid, ref stageObeliskEvent);
            Console.WriteLine("Stage: 'Obelisk' started");
        }
    }

}
