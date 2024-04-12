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
                out var spillSolution))
            return;

        _solutionSystem.SetCapacity((spillSolution.Value, spillSolution), FixedPoint2.MaxValue);
        _solutionSystem.SetCapacity((bloodSolution.Value, bloodSolution), bloodstream.MaxVolume);
        var volume = bloodstream.Volume > 0 ? bloodstream.Volume : bloodstream.MaxVolume;

        if (bloodstream.RegenCutoffVolume < 0)
            bloodstream.RegenCutoffVolume = bloodstream.MaxVolume;
        bloodstream.SpillSolution = spillSolution;
        bloodstream.BloodSolution = bloodSolution;

        //If we have a circulation comp, call the setup method on bloodCirculationSystem
        if (TryComp<VascularComponent>(bloodstreamEnt, out var bloodCircComp))
        {
            _vascularSystem.SetupCirculation(bloodstreamEnt, bloodstream, bloodCircComp, volume, bloodSolution.Value);
        }
        else
        {
            _solutionSystem.AddSolution((bloodSolution.Value, bloodSolution), new Solution(bloodstream.BloodReagent!, volume));
            bloodstream.BloodReagentId = new ReagentId(bloodstream.BloodReagent!, null);
            bloodstream.Volume = volume;
        }
        Dirty(bloodstreamEnt, bloodstream);
    }

    private void RegenBlood(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream)
    {
        //Don't passively regenerate blood if we are over the "healthy" volume
        if (bloodstream.Comp1.Regen == 0
            || bloodstream.Comp1.Volume >= bloodstream.Comp1.RegenCutoffVolume
            || !TryGetBloodSolution(bloodstream, out var bloodSolution))
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
                out var spillSolution))
            return;

        var bleedSol = _solutionSystem.SplitSolution(bloodSolution.Value, bloodstream.Comp1.Bloodloss);
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
        //Puddle spill implementation is serverside only so this will be abstract and only implemented on the server
        //TODO: Make sure to transfer DNA as well (serverside only too)
        Log.Debug($"PLACEHOLDER: A blood puddle should have been spawned for {ToPrettyString(bloodstream)}!");
    }


    public bool TryGetBloodStreamSolutions(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream,
        [NotNullWhen(true)]out Entity<SolutionComponent>? bloodSolution,
        [NotNullWhen(true)] out Entity<SolutionComponent>? spillSolution)
    {
        bloodSolution = null;
        spillSolution = null;
        return TryGetBloodSolution(bloodstream, out bloodSolution)
               && TryGetBloodSolution(bloodstream, out spillSolution);
    }

    public bool TryGetBloodSolution(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream,
        [NotNullWhen(true)]out Entity<SolutionComponent>? bloodSolution)
    {
        bloodSolution = null;
        if (_solutionSystem.TryGetSolution((bloodstream, bloodstream),
                BloodstreamComponent.BloodSolutionId, out bloodSolution))
            return true;
        Log.Error($"{ToPrettyString(bloodstream)} Does not have a solution with ID: {BloodstreamComponent.BloodSolutionId}, " +
                  $"which is required for bloodstream to function!");
        return false;
    }

    public bool TryGetSpillSolution(Entity<BloodstreamComponent, SolutionContainerManagerComponent> bloodstream,
        [NotNullWhen(true)]out Entity<SolutionComponent>? spillSolution)
    {
        spillSolution = null;
        if (_solutionSystem.TryGetSolution((bloodstream, bloodstream),
                BloodstreamComponent.SpillSolutionId, out spillSolution))
            return true;
        Log.Error($"{ToPrettyString(bloodstream)} Does not have a solution with ID: {BloodstreamComponent.SpillSolutionId}, " +
                  $"which is required for bloodstream to function!");
        return false;
    }

}
