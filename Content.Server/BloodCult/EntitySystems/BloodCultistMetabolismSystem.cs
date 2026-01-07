// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.BloodCult;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// Changes blood cultists' blood to Unholy Blood
/// </summary>
public sealed class BloodCultistMetabolismSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<BloodCultistComponent, ComponentInit>(OnCultistInit);
        SubscribeLocalEvent<BloodCultistComponent, ComponentShutdown>(OnCultistShutdown);
        // Note: ComponentRemove is handled by BloodCultRuleSystem, so we use ComponentShutdown and EntityTerminatingEvent instead
        SubscribeLocalEvent<BloodCultistComponent, EntityTerminatingEvent>(OnCultistTerminating);
        

    }
    
    public override void Shutdown()
    {
        base.Shutdown();
    }

    private void OnCultistInit(EntityUid uid, BloodCultistComponent component, ComponentInit args)
    {
        // Store the original blood type and change it to Unholy Blood
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            // Store the original blood type if not already stored
            if (string.IsNullOrEmpty(component.OriginalBloodReagent))
            {
                string originalBlood = "Blood"; // Default fallback
                
                // Try to get from prototype first
                if (TryGetPrototypeBloodReagent(uid, out var prototypeBlood) && !string.IsNullOrEmpty(prototypeBlood))
                {
                    originalBlood = prototypeBlood;
                }
                // Fallback to current blood reagent if available
                else if (bloodstream.BloodReferenceSolution.Contents.Count > 0)
                {
                    originalBlood = bloodstream.BloodReferenceSolution.Contents.First().Reagent.Prototype.ToString();
                }
                // Otherwise use default "Blood"
                
                component.OriginalBloodReagent = originalBlood;
            }
            
            // Change their blood type to Unholy Blood so they bleed it
            // Only if they have a valid bloodstream component
            try
            {
                var unholyBloodSolution = new Solution();
                unholyBloodSolution.AddReagent((ProtoId<ReagentPrototype>)"UnholyBlood", FixedPoint2.New(1));
                _bloodstream.ChangeBloodReagents((uid, bloodstream), unholyBloodSolution);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to change blood type to UnholyBlood for {ToPrettyString(uid)}: {ex}");
            }
        }
    }

    /// <summary>
    /// Handles cleanup when a cultist entity is being terminated.
    /// Also restores blood type if the component is still present (edge case where entity is deleted while still a cultist).
    /// </summary>
    private void OnCultistTerminating(EntityUid uid, BloodCultistComponent component, ref EntityTerminatingEvent args)
    {
        // Restore blood type if component is still present (edge case)
        // Normally this would be handled by OnCultistShutdown, but if the entity is being deleted
        // while still a cultist, we should restore the blood type here
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            string restoreReagent = "Blood"; // Default fallback
            
            // Try to use stored original blood reagent
            if (!string.IsNullOrEmpty(component.OriginalBloodReagent))
            {
                restoreReagent = component.OriginalBloodReagent;
            }
            // Fallback: try to get from prototype
            else if (TryGetPrototypeBloodReagent(uid, out var prototypeReagent) && !string.IsNullOrEmpty(prototypeReagent))
            {
                restoreReagent = prototypeReagent;
            }
            // Otherwise use default "Blood"
            
            // Restore the blood type with error handling
            try
            {
                var restoreSolution = new Solution();
                restoreSolution.AddReagent((ProtoId<ReagentPrototype>)restoreReagent, FixedPoint2.New(1));
                _bloodstream.ChangeBloodReagents((uid, bloodstream), restoreSolution);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to restore blood type to {restoreReagent} for {ToPrettyString(uid)} during termination: {ex}");
            }
        }
    }

    private void OnCultistShutdown(EntityUid uid, BloodCultistComponent component, ComponentShutdown args)
    {
        // Restore blood type to original blood reagent
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return; // No bloodstream, nothing to restore
        
        string restoreReagent = "Blood"; // Default fallback
        
        // Try to use stored original blood reagent
        if (!string.IsNullOrEmpty(component.OriginalBloodReagent))
        {
            restoreReagent = component.OriginalBloodReagent;
        }
        // Fallback: try to get from prototype
        else if (TryGetPrototypeBloodReagent(uid, out var prototypeReagent) && !string.IsNullOrEmpty(prototypeReagent))
        {
            restoreReagent = prototypeReagent;
        }
        // Otherwise use default "Blood"
        
        // Restore the blood type with error handling
        try
        {
                var restoreSolution = new Solution();
                restoreSolution.AddReagent((ProtoId<ReagentPrototype>)restoreReagent, FixedPoint2.New(1));
                _bloodstream.ChangeBloodReagents((uid, bloodstream), restoreSolution);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to restore blood type to {restoreReagent} for {ToPrettyString(uid)}: {ex}");
        }
    }

	private bool TryGetPrototypeBloodReagent(EntityUid uid, out string bloodReagent)
	{
		bloodReagent = "Blood"; // Default fallback

		try
		{
			var meta = MetaData(uid);
			if (meta.EntityPrototype == null)
				return false;

			if (!meta.EntityPrototype.TryGetComponent(_componentFactory.GetComponentName<BloodstreamComponent>(), out BloodstreamComponent? prototypeBloodstream))
				return false;

			// Only return the prototype blood reagent if it's not null or empty
			if (prototypeBloodstream.BloodReferenceSolution.Contents.Count > 0)
			{
				bloodReagent = prototypeBloodstream.BloodReferenceSolution.Contents.First().Reagent.Prototype.ToString();
				return true;
			}
			
			// If prototype has empty blood reagent, return false but keep default "Blood"
			return false;
		}
		catch (Exception ex)
		{
			Log.Warning($"Error getting prototype blood reagent for {ToPrettyString(uid)}: {ex}");
			return false;
		}
	}
}
