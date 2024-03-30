using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulatory.Components;
using Content.Shared.Medical.Circulatory.Prototypes;

namespace Content.Shared.Medical.Circulatory.Systems;

public sealed partial class BloodstreamSystem
{

    private void InitSolutions()
    {
        SubscribeLocalEvent<BloodstreamComponent, MapInitEvent>(OnBloodstreamMapInit,
            after: [typeof(SharedSolutionContainerSystem)]);
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
        Dirty(bloodstreamEnt, bloodstream);
    }

    private void TransferBloodToSpill(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream)
    {
        if (bloodstream.Comp1.Bloodloss == 0)
            return;
        if (!_solutionSystem.TryGetSolution((bloodstream, bloodstream),
                BloodstreamComponent.BloodSolutionId, out var bloodSolution))
        {
            Log.Error($"{ToPrettyString(bloodstream)} Does not have a solution with ID: {BloodstreamComponent.BloodSolutionId}, " +
                      $"which is required for bloodstream to function!");
            return;
        }

        if (!_solutionSystem.TryGetSolution((bloodstream, bloodstream),
                BloodstreamComponent.SpillSolutionId, out var spillSolution))
        {
            Log.Error($"{ToPrettyString(bloodstream)} Does not have a solution with ID: {BloodstreamComponent.SpillSolutionId}, " +
                      $"which is required for bloodstream to function!");
            return;
        }

        var bleedSol = _solutionSystem.SplitSolution(bloodSolution.Value, bloodstream.Comp1.Bloodloss);
        spillSolution.Value.Comp.Solution.AddSolution(bleedSol, _protoManager);
        if (spillSolution.Value.Comp.Solution.Volume > bloodstream.Comp1.BleedPuddleThreshold)
        {
            CreateBloodPuddle(bloodstream, spillSolution.Value.Comp.Solution);
        }
        _solutionSystem.RemoveAllSolution(spillSolution.Value);
    }

    //TODO: protected abstract this
    private void CreateBloodPuddle(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream,
        Solution spillSolution)
    {
        //Puddle spill implementation is serverside only so this will be abstract and only implemented on the server
        //TODO: Make sure to transfer DNA as well (serverside only too)
        Log.Debug($"PLACEHOLDER: A blood puddle should have been spawned for {ToPrettyString(bloodstream)}!");
    }
}
