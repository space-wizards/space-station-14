using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulatory.Components;

namespace Content.Shared.Medical.Circulatory.Systems;

public sealed partial class BloodstreamSystem
{
    public void AddBleed(Entity<BloodstreamComponent?> bloodstream, FixedPoint2 bleedToAdd)
    {
        if (bleedToAdd == 0 || !Resolve(bloodstream, ref bloodstream.Comp))
            return;
        bloodstream.Comp.Bloodloss += bleedToAdd;
        if (bloodstream.Comp.Bloodloss < 0)
            bloodstream.Comp.Bloodloss = 0;
        Dirty(bloodstream);
    }

    public void AddRegen(Entity<BloodstreamComponent?> bloodstream, FixedPoint2 regenToAdd)
    {
        if (regenToAdd == 0 || !Resolve(bloodstream, ref bloodstream.Comp))
            return;
        bloodstream.Comp.BloodRegen += regenToAdd;
        if (bloodstream.Comp.BloodRegen < 0)
            bloodstream.Comp.BloodRegen = 0;
        Dirty(bloodstream);
    }


    private void InitSolutions()
    {
    }

    private void UpdateSolutions(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream)
    {
        ApplyBleeds(bloodstream);
        RegenBlood(bloodstream);
    }



    private void RegenBlood(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream)
    {
        //Don't passively regenerate blood if we are over the "healthy" volume
        if (bloodstream.Comp1.BloodRegen == 0 || bloodstream.Comp1.BloodVolume >= bloodstream.Comp1.HealthyVolume)
            return;
        if (!_solutionSystem.TryGetSolution((bloodstream, bloodstream),
                BloodstreamComponent.BloodSolutionId, out var bloodSolution))
        {
            Log.Error($"{ToPrettyString(bloodstream)} Does not have a solution with ID: {BloodstreamComponent.BloodSolutionId}, " +
                      $"which is required for bloodstream to function!");
            return;
        }

        //Update the cached blood volume
        bloodstream.Comp1.BloodVolume += bloodstream.Comp1.BloodRegen;
        Dirty(bloodstream);

        bloodSolution.Value.Comp.Solution.Volume += bloodstream.Comp1.BloodRegen;
        _solutionSystem.UpdateChemicals(bloodSolution.Value);
    }


    private void ApplyBleeds(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream)
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

        //Update the cached blood volume
        bloodstream.Comp1.BloodVolume -= bloodstream.Comp1.Bloodloss;
        if (bloodstream.Comp1.BloodVolume < 0)
            bloodstream.Comp1.BloodVolume = 0;
        Dirty(bloodstream);

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
