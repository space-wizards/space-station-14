using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.HealthExaminable;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Sprite;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Body.Systems;

public sealed class BloodstreamSystem : SharedBloodstreamSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodstreamComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BloodstreamComponent, RefreshBloodEvent>(OnRefreshBlood);
    }

    // not sure if we can move this to shared or not
    // it would certainly help if SolutionContainer was documented
    // but since we usually don't add the component dynamically to entities we can keep this unpredicted for now
    private void OnComponentInit(Entity<BloodstreamComponent> entity, ref ComponentInit args)
    {
        if (!SolutionContainer.EnsureSolution(entity.Owner,
                entity.Comp.ChemicalSolutionName,
                out var chemicalSolution) ||
            !SolutionContainer.EnsureSolution(entity.Owner,
                entity.Comp.BloodSolutionName,
                out var bloodSolution) ||
            !SolutionContainer.EnsureSolution(entity.Owner,
                entity.Comp.BloodTemporarySolutionName,
                out var tempSolution))
            return;

        chemicalSolution.MaxVolume = entity.Comp.ChemicalMaxVolume;
        bloodSolution.MaxVolume = entity.Comp.BloodMaxVolume;
        tempSolution.MaxVolume = entity.Comp.BleedPuddleThreshold * 4; // give some leeway, for chemstream as well

        // Fill blood solution with BLOOD
        // The DNA string might not be initialized yet, but the reagent data gets updated in the GenerateDnaEvent subscription
        bloodSolution.AddReagent(new ReagentId(entity.Comp.BloodReagent, GetEntityBloodData(entity.Owner)), entity.Comp.BloodMaxVolume - bloodSolution.Volume);
    }

    // forensics is not predicted yet
    private void OnRefreshBlood(Entity<BloodstreamComponent> entity, ref RefreshBloodEvent args)
    {
        // TODO: this can fail due to component initialization order.
        // As an example, the SlimeBloodSystem raises this event
        // when a slime's appearance gets loaded.
        // However, appearance could be loaded before a blood solution
        // exists so this check could fail and not refresh the blood data
        // even though we want it to.
        if (SolutionContainer.ResolveSolution(entity.Owner, entity.Comp.BloodSolutionName, ref entity.Comp.BloodSolution, out var bloodSolution))
        {
            foreach (var reagent in bloodSolution.Contents)
            {
                var reagentData = reagent.Reagent.EnsureReagentData();
                reagentData.Clear();
                reagentData.AddRange(GetEntityBloodData(entity.Owner));
            }
        }
    }

    /// <summary>
    /// Get the reagent data for blood that a specific entity should have.
    /// </summary>
    public List<ReagentData> GetEntityBloodData(EntityUid uid)
    {
        // All blood always has DNA data, even if it's invalid, but color data is
        // only added whatsoever if the color is overridden in the event.
        var bloodData = new List<ReagentData>();
        var dnaData = new DnaData();

        if (TryComp<DnaComponent>(uid, out var donorComp) && donorComp.DNA != null)
            dnaData.DNA = donorComp.DNA;
        else
            dnaData.DNA = Loc.GetString("forensics-dna-unknown");
        bloodData.Add(dnaData);

        var ev = new BloodColorOverrideEvent { OverrideColor = null };
        RaiseLocalEvent(uid, ref ev);
        if (ev.OverrideColor != null)
        {
            var bloodColorData = new ReagentColorData
            {
                SubstanceColor = ev.OverrideColor.Value
            };
            bloodData.Add(bloodColorData);
        }

        return bloodData;
    }
}
