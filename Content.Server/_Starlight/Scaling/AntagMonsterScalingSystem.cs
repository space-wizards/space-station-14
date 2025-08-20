using Content.Shared._Starlight.Scaling.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Starlight.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Content.Server.StationRecords.Systems;
using Content.Shared.StationRecords;
using Content.Server.Station.Systems;
using Robust.Shared.Utility;
using Content.Shared._Starlight.Scaling;
using Content.Server.Ghost.Roles.Events;
using Content.Shared.Mobs;

namespace Content.Server._Starlight.Scaling;

public sealed class ScalingSystem : SharedScalingSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StationRecordsSystem _recordsSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private readonly Dictionary<EntityUid, double> _cachedPopulations = new();

    double _universalHealthWeight;
    int _populationBase;
    double _securityWeight;
    double _salvageWeight;
    double _centcomWeight;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AntagMonsterScalingComponent, MapInitEvent>(OnMobStartup);

        _cfg.OnValueChanged(StarlightCCVars.ScalingHealthWeight, UpdateUniversalHealthWeight, true);
    }

    private void UpdateUniversalHealthWeight(double universalHealthWeight)
    {
        _universalHealthWeight = universalHealthWeight;
    }

    private void OnMobStartup(EntityUid mob, AntagMonsterScalingComponent scalingComp, ref MapInitEvent args)
    {
        if (!TryComp(mob, out MobThresholdsComponent? thresholdsComp))
            return;

        scalingComp.OriginalThresholds = new();

        foreach (var threshold in thresholdsComp.Thresholds)
        {
            scalingComp.OriginalThresholds.Add(threshold.Key, threshold.Value);
        }

        var nullableStation = _stationSystem.GetStationInMap(Transform(mob).MapID);
        if (nullableStation == null)
            return;

        EntityUid station = nullableStation.Value;

        UpdatePopulation(station);

        if (scalingComp.IsScaled == false)
        {
            ApplyHealthScaling(station, scalingComp, thresholdsComp, _cachedPopulations, _universalHealthWeight);

            scalingComp.IsScaled = true;
        }
    }

    private void UpdatePopulation(EntityUid station)
    {
        _populationBase = _cfg.GetCVar(StarlightCCVars.ScalingPopulationBase);
        _securityWeight = _cfg.GetCVar(StarlightCCVars.ScalingSecurityWeight);
        _salvageWeight = _cfg.GetCVar(StarlightCCVars.ScalingSalvageWeight);
        _centcomWeight = _cfg.GetCVar(StarlightCCVars.ScalingCentcomWeight);

        double updatedPopulation = 0;

        var crewMembers = _recordsSystem.GetRecordsOfType<GeneralStationRecord>(station);

        foreach (var crewMember in crewMembers)
        {
            _prototypeManager.TryIndex(crewMember.Item2.JobPrototype, out JobPrototype? job);

            if (job == null)
                continue;

            if (job.Supervisors.Contains("hos")
            || job.Name.Contains("Security"))
            {
                updatedPopulation += 1 * _securityWeight;
            }
            if (job.Name.Contains("salvage"))
            {
                updatedPopulation += 1 * _salvageWeight;
            }
            if (job.Supervisors.Contains("centcom")
                && !job.Name.Contains("Magistrate")
                && !job.Name.Contains("Representative"))
            {
                updatedPopulation += 1 * _centcomWeight;
            }
        }

        _cachedPopulations.GetOrNew(station);
        _cachedPopulations[station] = updatedPopulation - _populationBase;
    }
}