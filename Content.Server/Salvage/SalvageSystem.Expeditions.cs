using System.Linq;
using System.Threading;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Salvage;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    /*
     * Handles setup / teardown of salvage expeditions.
     */

    private const int MissionLimit = 5;

    private readonly JobQueue _salvageQueue = new();
    private readonly List<(SpawnSalvageMissionJob Job, CancellationTokenSource CancelToken)> _salvageJobs = new();
    private const double SalvageJobTime = 0.002;

    private float _cooldown;
    private float _failedCooldown;

    private void InitializeExpeditions()
    {
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ComponentInit>(OnSalvageConsoleInit);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, EntParentChangedMessage>(OnSalvageConsoleParent);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ClaimSalvageMessage>(OnSalvageClaimMessage);

        SubscribeLocalEvent<SalvageExpeditionDataComponent, EntityUnpausedEvent>(OnDataUnpaused);

        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentShutdown>(OnExpeditionShutdown);
        SubscribeLocalEvent<SalvageExpeditionComponent, EntityUnpausedEvent>(OnExpeditionUnpaused);

        SubscribeLocalEvent<SalvageStructureComponent, ExaminedEvent>(OnStructureExamine);

        _cooldown = _configurationManager.GetCVar(CCVars.SalvageExpeditionCooldown);
        _failedCooldown = _configurationManager.GetCVar(CCVars.SalvageExpeditionFailedCooldown);
        _configurationManager.OnValueChanged(CCVars.SalvageExpeditionCooldown, SetCooldownChange);
        _configurationManager.OnValueChanged(CCVars.SalvageExpeditionFailedCooldown, SetFailedCooldownChange);
    }

    private void ShutdownExpeditions()
    {
        _configurationManager.UnsubValueChanged(CCVars.SalvageExpeditionCooldown, SetCooldownChange);
        _configurationManager.UnsubValueChanged(CCVars.SalvageExpeditionFailedCooldown, SetFailedCooldownChange);
    }

    private void SetCooldownChange(float obj)
    {
        // Update the active cooldowns if we change it.
        var diff = obj - _cooldown;

        var query = AllEntityQuery<SalvageExpeditionDataComponent>();

        while (query.MoveNext(out var comp))
        {
            comp.NextOffer += TimeSpan.FromSeconds(diff);
        }

        _cooldown = obj;
    }

    private void SetFailedCooldownChange(float obj)
    {
        var diff = obj - _failedCooldown;

        var query = AllEntityQuery<SalvageExpeditionDataComponent>();

        while (query.MoveNext(out var comp))
        {
            comp.NextOffer += TimeSpan.FromSeconds(diff);
        }

        _failedCooldown = obj;
    }

    private void OnExpeditionShutdown(EntityUid uid, SalvageExpeditionComponent component, ComponentShutdown args)
    {
        component.Stream?.Stop();

        foreach (var (job, cancelToken) in _salvageJobs.ToArray())
        {
            if (job.Station == component.Station)
            {
                cancelToken.Cancel();
                _salvageJobs.Remove((job, cancelToken));
            }
        }

        if (Deleted(component.Station))
            return;

        // Finish mission
        if (TryComp<SalvageExpeditionDataComponent>(component.Station, out var data))
        {
            FinishExpedition(data, component, null);
        }
    }

    private void OnDataUnpaused(EntityUid uid, SalvageExpeditionDataComponent component, ref EntityUnpausedEvent args)
    {
        component.NextOffer += args.PausedTime;
    }

    private void OnExpeditionUnpaused(EntityUid uid, SalvageExpeditionComponent component, ref EntityUnpausedEvent args)
    {
        component.EndTime += args.PausedTime;
    }

    private void UpdateExpeditions()
    {
        var currentTime = _timing.CurTime;
        _salvageQueue.Process();

        foreach (var (job, cancelToken) in _salvageJobs.ToArray())
        {
            switch (job.Status)
            {
                case JobStatus.Finished:
                    _salvageJobs.Remove((job, cancelToken));
                    break;
            }
        }

        foreach (var comp in EntityQuery<SalvageExpeditionDataComponent>())
        {
            // Update offers
            if (comp.NextOffer > currentTime || comp.Claimed)
                continue;

            comp.Cooldown = false;
            comp.NextOffer += TimeSpan.FromSeconds(_cooldown);
            GenerateMissions(comp);
            UpdateConsoles(comp);
        }
    }

    private void FinishExpedition(SalvageExpeditionDataComponent component, SalvageExpeditionComponent expedition, EntityUid? shuttle)
    {
        // Finish mission cleanup.
        switch (expedition.MissionParams.MissionType)
        {
            // Handles the mining taxation.
            case SalvageMissionType.Mining:
                expedition.Completed = true;

                if (shuttle != null && TryComp<SalvageMiningExpeditionComponent>(expedition.Owner, out var mining))
                {
                    var xformQuery = GetEntityQuery<TransformComponent>();
                    var entities = new List<EntityUid>();
                    MiningTax(entities, shuttle.Value, mining, xformQuery);

                    var tax = GetMiningTax(expedition.MissionParams.Difficulty);
                    _random.Shuffle(entities);

                    // TODO: urgh this pr is already taking so long I'll do this later
                    for (var i = 0; i < Math.Ceiling(entities.Count * tax); i++)
                    {
                        // QueueDel(entities[i]);
                    }
                }

                break;
        }

        // Payout already handled elsewhere.
        if (expedition.Completed)
        {
            _sawmill.Debug($"Completed mission {expedition.MissionParams.MissionType} with seed {expedition.MissionParams.Seed}");
            component.NextOffer = _timing.CurTime + TimeSpan.FromSeconds(_cooldown);
            Announce(expedition.Owner, Loc.GetString("salvage-expedition-mission-completed"));
        }
        else
        {
            _sawmill.Debug($"Failed mission {expedition.MissionParams.MissionType} with seed {expedition.MissionParams.Seed}");
            component.NextOffer = _timing.CurTime + TimeSpan.FromSeconds(_failedCooldown);
            Announce(expedition.Owner, Loc.GetString("salvage-expedition-mission-failed"));
        }

        component.ActiveMission = 0;
        component.Cooldown = true;
        UpdateConsoles(component);
    }

    /// <summary>
    /// Deducts ore tax for mining.
    /// </summary>
    private void MiningTax(List<EntityUid> entities, EntityUid entity, SalvageMiningExpeditionComponent mining, EntityQuery<TransformComponent> xformQuery)
    {
        if (!mining.ExemptEntities.Contains(entity))
        {
            entities.Add(entity);
        }

        var xform = xformQuery.GetComponent(entity);
        var children = xform.ChildEnumerator;

        while (children.MoveNext(out var child))
        {
            MiningTax(entities, child.Value, mining, xformQuery);
        }
    }

    private void GenerateMissions(SalvageExpeditionDataComponent component)
    {
        component.Missions.Clear();
        var configs = Enum.GetValues<SalvageMissionType>().ToList();

        if (configs.Count == 0)
            return;

        // Temporarily removed coz it SUCKS
        configs.Remove(SalvageMissionType.Mining);

        for (var i = 0; i < MissionLimit; i++)
        {
            _random.Shuffle(configs);
            var rating = (DifficultyRating) i;

            foreach (var config in configs)
            {
                var mission = new SalvageMissionParams()
                {
                    Index = component.NextIndex,
                    MissionType = config,
                    Seed = _random.Next(),
                    Difficulty = rating,
                };

                component.Missions[component.NextIndex++] = mission;
                break;
            }
        }
    }

    private SalvageExpeditionConsoleState GetState(SalvageExpeditionDataComponent component)
    {
        var missions = component.Missions.Values.ToList();
        return new SalvageExpeditionConsoleState(component.NextOffer, component.Claimed, component.Cooldown, component.ActiveMission, missions);
    }

    private void SpawnMission(SalvageMissionParams missionParams, EntityUid station)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new SpawnSalvageMissionJob(
            SalvageJobTime,
            EntityManager,
            _timing,
            _mapManager,
            _prototypeManager,
            _tileDefManager,
            _biome,
            _dungeon,
            this,
            station,
            missionParams,
            cancelToken.Token);

        _salvageJobs.Add((job, cancelToken));
        _salvageQueue.EnqueueJob(job);
    }

    private void OnStructureExamine(EntityUid uid, SalvageStructureComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("salvage-expedition-structure-examine"));
    }
}
