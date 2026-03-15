using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Body.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.BloodCult.EntityEffects;

/// <summary>
/// System that handles the BleedUnholyBlood entity effect, which changes a cultist's blood to the cult blood reagent (e.g. Unholy Blood).
/// </summary>
public sealed partial class BleedUnholyBloodEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, BleedUnholyBlood>
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly BloodCultRuleSystem _bloodCultRule = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<BleedUnholyBlood> args)
    {
        var bloodstream = entity.Comp;

        // Store their original blood type if not already stored
        if (!TryComp<EdgeEssentiaBloodComponent>(entity, out var edgeEssentiaComp))
        {
            edgeEssentiaComp = AddComp<EdgeEssentiaBloodComponent>(entity);
            ProtoId<ReagentPrototype> originalBlood = "Blood";
            if (TryGetPrototypeBloodReagent(entity, out var protoBlood))
                originalBlood = protoBlood;
            else if (bloodstream.BloodReferenceSolution.Contents.Count > 0)
            {
                // Get first reagent from reference solution
                originalBlood = bloodstream.BloodReferenceSolution.Contents.First().Reagent.Prototype;
            }

            edgeEssentiaComp.OriginalBloodReagent = originalBlood.ToString();
        }

        // Change their blood type to cult blood so that when they bleed, it comes out as the configured reagent (e.g. Unholy Blood).
        // This happens every metabolism tick, ensuring their blood type stays set while Edge Essentia is active.
        var cultBloodReagent = _bloodCultRule.TryGetActiveRule(out var ruleComp) ? ruleComp.CultBloodReagent : "UnholyBlood";
        var cultBloodSolution = new Solution();
        cultBloodSolution.AddReagent((ProtoId<ReagentPrototype>)cultBloodReagent, FixedPoint2.New(1));
        _bloodstream.ChangeBloodReagents((entity, bloodstream), cultBloodSolution);
    }

    private bool TryGetPrototypeBloodReagent(EntityUid uid, out ProtoId<ReagentPrototype> bloodReagent)
    {
        bloodReagent = default!;

        if (!TryComp(uid, out MetaDataComponent? meta) || meta.EntityPrototype == null)
            return false;

        var componentFactory = EntityManager.ComponentFactory;
        if (!meta.EntityPrototype.TryGetComponent(componentFactory.GetComponentName<BloodstreamComponent>(), out BloodstreamComponent? prototypeBloodstream))
            return false;

        // Get first reagent from reference solution
        if (prototypeBloodstream.BloodReferenceSolution.Contents.Count > 0)
        {
            bloodReagent = prototypeBloodstream.BloodReferenceSolution.Contents.First().Reagent.Prototype;
            return true;
        }
        return false;
    }
}

/// <summary>
/// Component to track the original blood type of an entity affected by Edge Essentia
/// and how much Unholy Blood they've bled for the ritual pool.
/// </summary>
[RegisterComponent]
public sealed partial class EdgeEssentiaBloodComponent : Component
{
    /// <summary>
    /// The original blood reagent before Edge Essentia changed it
    /// </summary>
    [DataField]
    public string OriginalBloodReagent = "Blood";

    /// <summary>
    /// Tracks the last amount of Unholy Blood in the temporary solution to detect new bleeding
    /// </summary>
    [DataField]
    public FixedPoint2 LastTrackedBloodAmount = FixedPoint2.Zero;
}
