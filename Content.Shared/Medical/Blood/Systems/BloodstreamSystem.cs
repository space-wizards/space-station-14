using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Blood.Components;
using Content.Shared.Medical.Blood.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Blood.Systems;

public sealed class BloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly VascularSystem _vascularSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BloodstreamComponent, MapInitEvent>(OnBloodstreamMapInit,
            after: [typeof(SharedSolutionContainerSystem)]);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BloodstreamComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var bloodstreamComp, out var solMan))
        {
            if (_gameTiming.CurTime < bloodstreamComp.NextUpdate)
                continue;
            bloodstreamComp.NextUpdate += bloodstreamComp.UpdateInterval;
            ApplyBleeds((uid, bloodstreamComp, solMan));
            RegenBlood((uid, bloodstreamComp, solMan));

            var ev = new BloodstreamUpdatedEvent((uid, bloodstreamComp, solMan));
            RaiseLocalEvent(uid, ref ev);
        }
    }

    public void ApplyBleed(Entity<BloodstreamComponent?> bloodstream, FixedPoint2 bleedToAdd)
    {
        if (bleedToAdd == 0 || !Resolve(bloodstream, ref bloodstream.Comp))
            return;
        bloodstream.Comp.Bloodloss += bleedToAdd;
        if (bloodstream.Comp.Bloodloss < 0)
            bloodstream.Comp.Bloodloss = 0;
        Dirty(bloodstream);
    }

    public void ApplyRegen(Entity<BloodstreamComponent?> bloodstream, FixedPoint2 regenToAdd)
    {
        if (regenToAdd == 0 || !Resolve(bloodstream, ref bloodstream.Comp))
            return;
        bloodstream.Comp.Regen += regenToAdd;
        if (bloodstream.Comp.Regen < 0)
            bloodstream.Comp.Regen = 0;
        Dirty(bloodstream);
    }

    private void OnBloodstreamMapInit(EntityUid bloodstreamEnt, BloodstreamComponent bloodstream, ref MapInitEvent args)
    {

        if (!TryGetBloodStreamSolutions(
                (bloodstreamEnt, bloodstream, Comp<SolutionContainerManagerComponent>(bloodstreamEnt)),
                out var bloodSolution,
                out var bloodReagentSolution,
                out var spillSolution))
            return;

        _solutionSystem.SetCapacity((bloodSolution.Value, bloodSolution), bloodstream.MaxVolume);
        _solutionSystem.SetCapacity((bloodReagentSolution.Value, bloodReagentSolution), bloodstream.MaxVolume);
        _solutionSystem.SetCapacity((spillSolution.Value, spillSolution), FixedPoint2.MaxValue);

        var volume = bloodstream.Volume > 0 ? bloodstream.Volume : bloodstream.MaxVolume;

        if (bloodstream.RegenCutoffVolume < 0)
            bloodstream.RegenCutoffVolume = bloodstream.MaxVolume;
        bloodstream.SpillSolutionEnt = spillSolution;
        bloodstream.BloodSolutionEnt = bloodSolution;

        //If we have a circulation comp, call the setup method on bloodCirculationSystem
        if (TryComp<VascularSystemComponent>(bloodstreamEnt, out var bloodCircComp))
        {
            _vascularSystem.SetupCirculation(bloodstreamEnt, bloodstream, bloodCircComp, volume, bloodSolution.Value);
        }
        else
        {
            if (bloodstream.BloodReagent == null)
            {
                throw new Exception($"Blood reagent is not defined for {ToPrettyString(bloodstreamEnt)}");
            }
            _solutionSystem.AddSolution((bloodSolution.Value, bloodSolution),
                new Solution(bloodstream.BloodReagent, volume));
            bloodstream.BloodReagentId = new ReagentId(bloodstream.BloodReagent, null);
            bloodstream.Volume = volume;
        }
        Dirty(bloodstreamEnt, bloodstream);
    }

    private void RegenBlood(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream)
    {
        //Don't passively regenerate blood if we are over the "healthy" volume
        if (bloodstream.Comp1.Regen == 0
            || bloodstream.Comp1.Volume >= bloodstream.Comp1.RegenCutoffVolume
            || !_solutionSystem.TryGetSolution((bloodstream, bloodstream),
                BloodstreamComponent.BloodSolutionId, out var bloodSolution, true))
            return;

        bloodSolution.Value.Comp.Solution.Volume += bloodstream.Comp1.Regen;
        _solutionSystem.UpdateChemicals(bloodSolution.Value);

        //Update the cached blood volume
        bloodstream.Comp1.Volume = bloodSolution.Value.Comp.Solution.Volume;
        Dirty(bloodstream);
    }

    private void ApplyBleeds(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream)
    {
        if (bloodstream.Comp1.Bloodloss == 0 || !TryGetBloodStreamSolutions(bloodstream,
                out var bloodSolution,
                out var reagentSolution,
                out var spillSolution))
            return;

        var bleedSol = _solutionSystem.SplitSolution(bloodSolution.Value, bloodstream.Comp1.Bloodloss);
        //Get the disolved reagent loss amount by getting the bleed percentage and then multiplying it by the disolved reagent volume
        var reagentLossAmount = reagentSolution.Value.Comp.Solution.Volume * (bloodstream.Comp1.Bloodloss / bloodSolution.Value.Comp.Solution.MaxVolume);

        //Not sure if it's the best idea to just spill the dissolved reagents straight into the blood puddle but we don't have
        //functionality for a reagent holding other dissolved reagents
        var lostDissolvedReagents = _solutionSystem.SplitSolution(reagentSolution.Value, reagentLossAmount);
        spillSolution.Value.Comp.Solution.AddSolution(lostDissolvedReagents, _protoManager);

        spillSolution.Value.Comp.Solution.AddSolution(bleedSol, _protoManager);
        if (spillSolution.Value.Comp.Solution.Volume > bloodstream.Comp1.BleedPuddleThreshold)
        {
            CreateBloodPuddle(bloodstream, spillSolution.Value.Comp.Solution);
        }
        _solutionSystem.RemoveAllSolution(spillSolution.Value);
        //Update the cached blood volume
        bloodstream.Comp1.Volume = bloodSolution.Value.Comp.Solution.Volume;
        Dirty(bloodstream);
    }

    //TODO: protected abstract this
    private void CreateBloodPuddle(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream,
        Solution spillSolution)
    {
        //TODO: placeholder, clear the reagent to prevent mispredicts
        spillSolution.RemoveAllSolution();
        //Puddle spill implementation is serverside only so this will be abstract and only implemented on the server
        //TODO: Make sure to transfer DNA as well (serverside only too)
        Log.Debug($"PLACEHOLDER: A blood puddle should have been spawned for {ToPrettyString(bloodstream)}!");
    }


    public bool TryGetBloodStreamSolutions(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream,
        [NotNullWhen(true)]out Entity<SolutionComponent>? bloodSolution,
        [NotNullWhen(true)]out Entity<SolutionComponent>? bloodReagentSolution,
        [NotNullWhen(true)] out Entity<SolutionComponent>? spillSolution)
    {
        bloodSolution = null;
        spillSolution = null;
        bloodReagentSolution = null;
        return _solutionSystem.TryGetSolution((bloodstream, bloodstream),
                   BloodstreamComponent.BloodSolutionId, out bloodSolution, true)
               && _solutionSystem.TryGetSolution((bloodstream, bloodstream),
                   BloodstreamComponent.DissolvedReagentSolutionId, out bloodReagentSolution, true)
               && _solutionSystem.TryGetSolution((bloodstream, bloodstream),
                   BloodstreamComponent.SpillSolutionId, out spillSolution, true);
    }

    public bool TryGetBloodSolution(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream,
        [NotNullWhen(true)]out Entity<SolutionComponent>? bloodSolution)
    {
         return _solutionSystem.TryGetSolution((bloodstream, bloodstream),
                BloodstreamComponent.BloodSolutionId, out bloodSolution, true);
    }

}
