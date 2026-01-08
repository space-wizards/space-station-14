// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.BloodCult.EntityEffects;

/// <summary>
/// Makes an entity bleed Unholy Blood instead of their normal blood type while they metabolize Edge Essentia.
/// Changes what blood they bleed out, not their internal blood.
/// </summary>
public sealed partial class BleedUnholyBlood : EntityEffectBase<BleedUnholyBlood>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-bleed-unholy-blood", ("chance", Probability));
}

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class BleedUnholyBloodEntityEffectSystem : EntityEffectSystem<BloodstreamComponent, BleedUnholyBlood>
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<BleedUnholyBlood> args)
    {
        // Verify target entity exists (could be deleted during metabolism processing)
        if (!Exists(entity))
            return;

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

        // Change their blood type to Unholy Blood so that when they bleed, it comes out as Unholy Blood
        // This happens every metabolism tick, ensuring their blood type stays as UnholyBlood while Edge Essentia is active
        var unholyBloodSolution = new Solution();
        unholyBloodSolution.AddReagent((ProtoId<ReagentPrototype>)"UnholyBlood", FixedPoint2.New(1));
        _bloodstream.ChangeBloodReagents((entity, bloodstream), unholyBloodSolution);
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
