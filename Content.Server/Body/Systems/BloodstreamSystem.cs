using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : SharedBloodstreamSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodstreamComponent, GenerateDnaEvent>(OnDnaGenerated);
    }

    // not sure if we can move this to shared or not
    // it would certainly help if SolutionContainer was documented
    // but since we usually don't add the component dynamically to entities we can keep this unpredicted for now
    private void OnComponentInit(Entity<BloodstreamComponent> entity, ref ComponentInit args)
    {
        if (!SolutionContainer.EnsureSolution(entity.Owner,
                entity.Comp.BloodSolutionName,
                out var bloodSolution) ||
            !SolutionContainer.EnsureSolution(entity.Owner,
                entity.Comp.BloodTemporarySolutionName,
                out var tempSolution))
            return;

        bloodSolution.MaxVolume = entity.Comp.BloodReferenceSolution.Volume * entity.Comp.MaxVolumeModifier;
        tempSolution.MaxVolume = entity.Comp.BleedPuddleThreshold * 4; // give some leeway, for chemstream as well
        entity.Comp.BloodReferenceSolution.SetReagentData(GetEntityBloodData((entity, entity.Comp)));

        // Fill blood solution with BLOOD
        // The DNA string might not be initialized yet, but the reagent data gets updated in the GenerateDnaEvent subscription
        var solution = entity.Comp.BloodReferenceSolution.Clone();
        solution.ScaleTo(entity.Comp.BloodReferenceSolution.Volume - bloodSolution.Volume);
        bloodSolution.AddSolution(solution, PrototypeManager);
    }

    // forensics is not predicted yet
    private void OnDnaGenerated(Entity<BloodstreamComponent> entity, ref GenerateDnaEvent args)
    {
        if (SolutionContainer.ResolveSolution(entity.Owner, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution))
        {
            var data = NewEntityBloodData(entity);
            entity.Comp.BloodReferenceSolution.SetReagentData(data);

            foreach (var reagent in bloodSolution.Contents)
            {
                List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();
                reagentData.RemoveAll(x => x is DnaData);
                reagentData.AddRange(data);
            }
        }
        else
            Log.Error("Unable to set bloodstream DNA, solution entity could not be resolved");
    }
}
