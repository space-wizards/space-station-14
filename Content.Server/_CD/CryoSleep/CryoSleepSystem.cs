using System.Linq;
using Content.Server.Climbing;
using Content.Server.Mind;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Destructible;
using Content.Shared.Mind;
using Content.Server.EUI;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusEffect;
using Content.Shared.Verbs;
using Content.Server.GameTicking;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Enums;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Content.Server.Warps;

namespace Content.Server.CryoSleep;

public sealed class CryoSleepSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;
    [Dependency] private readonly SharedJobSystem _sharedJobSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoSleepComponent, ComponentInit>(ComponentInit);
        SubscribeLocalEvent<CryoSleepComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        SubscribeLocalEvent<CryoSleepComponent, DestructionEventArgs>((e, c, _) => EjectBody(e, c));
    }       

    private void ComponentInit(EntityUid uid, CryoSleepComponent component, ComponentInit args)
    {
        component.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, "body_container");
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        // Ensure they have a job, ie. stuff like skeletons (hopefully?) shouldn't be yoinked. 
        if (args.JobId == null)
            return;

        var validPods = new List<CryoSleepComponent>();
        foreach (var component in EntityQuery<CryoSleepComponent>().ToArray())
        {
            if (component.DoSpawns == true)
                validPods.Add(component);
        }
        _random.Shuffle(validPods);

        if (!validPods.Any())
            return;

        var pod = validPods.First();
        _audio.PlayPvs(pod.ArrivalSound, pod.Owner);

        InsertBody(pod.Owner, args.Mob, pod);
        var duration = _random.NextFloat(pod.InitialSleepDurationRange.X, pod.InitialSleepDurationRange.Y);
        _statusEffects.TryAddStatusEffect<SleepingComponent>(args.Mob, "ForcedSleep", TimeSpan.FromSeconds(duration), false);
    }

    private bool InsertBody(EntityUid uid, EntityUid? toInsert, CryoSleepComponent component)
    {
        if (toInsert == null || IsOccupied(component))
            return false;

        if (!HasComp<MobStateComponent>(toInsert.Value))
            return false;

        var inserted = component.BodyContainer.Insert(toInsert.Value, EntityManager);

        return inserted;
    }

    public bool RespawnUser(EntityUid? toInsert, CryoSleepComponent component, bool force)
    {
        if (toInsert == null)
            return false;

        if (IsOccupied(component) && !force)
            return false;

        if (_mindSystem.TryGetMind(toInsert.Value, out var mind, out var mindComp))
        {
            var session = mindComp.Session;
            if (session != null && session.Status == SessionStatus.Disconnected)
            {
                InsertBody(toInsert.Value, component.Owner, component);
                return true;
            }
        }

        var success = component.BodyContainer.Insert(toInsert.Value, EntityManager);

        if (success && mindComp?.Session != null)
        {
            _euiManager.OpenEui(new CryoSleepEui(mind, this), mindComp.Session);
        }

        return success;
    }
    public void CryoStoreBody(EntityUid mindId)
    {
        if (!_sharedJobSystem.MindTryGetJob(mindId, out _, out var prototype))
            return;

        if (!TryComp<MindComponent>(mindId, out var mind))
            return;

        var body = mind.CurrentEntity;
        var job = prototype;

        _gameTicker.OnGhostAttempt(mindId, false, true, mind: mind);
        EntityManager.DeleteEntity(body);

        // This is awful, it feels awful, and I hate it. Warp points are really the only thing I can confirm exists on every station, though CC might cause issues so ideally I'd use one with a var set
        var query = EntityQuery<WarpPointComponent>().ToArray();

        var entity = query.First();

        // sets job slot
        var xform = Transform(entity.Owner);

        if (!xform.GridUid.HasValue)
            return;

        var station = _stationSystem.GetOwningStation(xform.GridUid.Value);
        if (!station.HasValue)
           return;

        if (job == null)
            return;

        _stationJobsSystem.TryGetJobSlot(station.Value, job, out var amount);
        if (!amount.HasValue)
            return;

        _stationJobsSystem.TrySetJobSlot(station.Value, job, (int) amount.Value + 1, true);
    }

    private bool EjectBody(EntityUid pod, CryoSleepComponent component)
    {
        if (!IsOccupied(component))
            return false;

        var toEject = component.BodyContainer.ContainedEntity;
        if (toEject == null)
            return false;

        component.BodyContainer.Remove(toEject.Value);
        _climb.ForciblySetClimbing(toEject.Value, pod);

        return true;
    }

    private bool IsOccupied(CryoSleepComponent component)
    {
        return component.BodyContainer.ContainedEntity != null;
    }

    private void AddAlternativeVerbs(EntityUid uid, CryoSleepComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Eject somebody verb
        if (IsOccupied(component))
        {
            AlternativeVerb verb = new()
            {
                Act = () => EjectBody(component.Owner, component),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("medical-scanner-verb-noun-occupant")
            };
            args.Verbs.Add(verb);
        }

        // Insert self verb
        if (!IsOccupied(component) &&
            _actionBlocker.CanMove(args.User))
        {
            AlternativeVerb verb = new()
            {
                Act = () => RespawnUser(args.User, component, false),
                Category = VerbCategory.Insert,
                Text = Loc.GetString("medical-scanner-verb-enter")
            };
            args.Verbs.Add(verb);
        }
    }
}
