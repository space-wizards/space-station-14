using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulatory.Components;
using Content.Shared.Medical.Circulatory.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Circulatory.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class BloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;


    //TODO: Cvar this!
    private TimeSpan _updateInterval = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BloodstreamComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var bloodstreamComp, out var solMan))
        {
            if (_gameTiming.CurTime < bloodstreamComp.NextUpdate)
                continue;
            bloodstreamComp.NextUpdate += _updateInterval;
            UpdateSolutions((uid, bloodstreamComp, solMan));
        }
    }


    /// <inheritdoc/>
    public override void Initialize()
    {
        _updateInterval = TimeSpan.FromSeconds(1.0f);

        SubscribeLocalEvent<BloodstreamComponent, MapInitEvent>(OnBloodstreamMapInit,
            after: [typeof(SharedSolutionContainerSystem)]);
        InitSolutions();
    }

    private void OnBloodstreamMapInit(EntityUid bloodstreamEnt, BloodstreamComponent bloodstream, ref MapInitEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(bloodstreamEnt, out var solMan))
        {
            Log.Error($"{ToPrettyString(bloodstreamEnt)} does not have a solution manager, but is using bloodstream. " +
                      $"Make sure that SolutionContainerManager is defined as a component in YAML.");
            return;
        }

        Entity<SolutionComponent>? bloodSolution = default;
        if (!_solutionSystem.ResolveSolution((bloodstreamEnt, solMan), BloodstreamComponent.BloodSolutionId,
                ref bloodSolution))
        {
            Log.Error($"{ToPrettyString(bloodstreamEnt)} does not have a solution with ID " +
                      $"{BloodstreamComponent.BloodSolutionId}. " +
                     $"Make sure that {BloodstreamComponent.BloodSolutionId} is added to SolutionContainerManager in YAML");
            return;
        }
        Entity<SolutionComponent>? spillSolution = default;
        if (!_solutionSystem.ResolveSolution((bloodstreamEnt, solMan), BloodstreamComponent.SpillSolutionId,
                ref spillSolution))
        {
            Log.Error($"{ToPrettyString(bloodstreamEnt)} does not have a solution with ID " +
                      $"{BloodstreamComponent.SpillSolutionId}. " +
                      $"Make sure that {BloodstreamComponent.SpillSolutionId} is added to SolutionContainerManager in YAML.");
            return;
        }

        var bloodDef = _protoManager.Index<BloodDefinitionPrototype>(bloodstream.BloodDefinition);
        var bloodType = GetInitialBloodType((bloodstreamEnt, bloodstream), bloodDef);

        _solutionSystem.SetCapacity((spillSolution.Value, spillSolution), FixedPoint2.MaxValue);
        _solutionSystem.SetCapacity((bloodSolution.Value, bloodSolution), bloodstream.MaxVolume);
        _solutionSystem.AddSolution((bloodSolution.Value, bloodSolution),
            CreateBloodSolution(bloodType, bloodDef, bloodstream.HealthyVolume));
        AddAllowedAntigens((bloodstreamEnt, bloodstream),GetAntigensForBloodType(bloodType));

        bloodstream.SpillSolution = spillSolution;
        bloodstream.BloodSolution = spillSolution;
        bloodstream.BloodType = bloodType.ID;
        bloodstream.BloodReagent = bloodDef.WholeBloodReagent;
        bloodstream.BloodVolume = bloodstream.MaxVolume;
        if (bloodstream.RegenTargetVolume < 0)
        {
            bloodstream.RegenTargetVolume = bloodstream.HealthyVolume;
        }
        Dirty(bloodstreamEnt, bloodstream);
    }
}
