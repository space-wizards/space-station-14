using Content.Server.BloodCult.EntityEffects;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Body.Components;
using Content.Shared.BloodCult.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// System that manages restoring original blood types after Edge Essentia wears off
/// and tracks Unholy Blood being bled out for the ritual pool.
/// </summary>
public sealed class EdgeEssentiaBloodSystem : EntitySystem
{
	[Dependency] private readonly IGameTiming _timing = default!;
	[Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
	[Dependency] private readonly BloodstreamSystem _bloodstream = default!;
	[Dependency] private readonly BloodCultRuleSystem _bloodCultRule = default!;

	private TimeSpan _nextUpdate = TimeSpan.Zero;
	private bool _bloodCultRuleActive = false;

	public override void Update(float frameTime)
	{
		base.Update(frameTime);

		var curTime = _timing.CurTime;
		if (curTime < _nextUpdate)
			return;

		_nextUpdate = curTime + TimeSpan.FromSeconds(1);

		// Check all entities with EdgeEssentiaBloodComponent
		var query = EntityQueryEnumerator<EdgeEssentiaBloodComponent, BloodstreamComponent>();
		
		// Early exit if no entities need processing (zero-cost when no cultists are active)
		if (!query.MoveNext(out var uid, out var edgeEssentia, out var bloodstream))
			return;

		// At least one entity exists with EdgeEssentiaBloodComponent
		// Check if BloodCult game rule is active (only needed if we're tracking blood loss)
		var cultBloodReagent = "UnholyBlood"; //default to Unholy Blood if no rule is active. This is a fallback for if edge essentia ever happens to exist without a blood cult gamerule
		_bloodCultRuleActive = _bloodCultRule.TryGetActiveRule(out var ruleComp);
		if (_bloodCultRuleActive)
			cultBloodReagent = ruleComp.CultBloodReagent;

		// Process the first entity
		do
		{
			// Verify entity still exists (could be deleted during processing)
			if (!Exists(uid))
				continue;

			// Track how much cult blood they're bleeding out (only if cult rule is active)
			// Check if their blood reference solution contains the cult blood reagent
			var hasCultBlood = false;
			foreach (var (reagentId, _) in bloodstream.BloodReferenceSolution.Contents)
			{
				if (reagentId.Prototype == cultBloodReagent)
				{
					hasCultBlood = true;
					break;
				}
			}
			if (_bloodCultRuleActive && hasCultBlood)
			{
				TrackUnholyBloodLoss(uid, edgeEssentia, bloodstream, cultBloodReagent);
			}

			// Check if they still have Edge Essentia in their system
			if (HasEdgeEssentia(uid, bloodstream))
				continue;

			// Verify entity still exists before modifying blood type
			if (!Exists(uid))
				continue;

			// No Edge Essentia left - restore their original blood type and remove the component
			var restoreSolution = new Solution();
			restoreSolution.AddReagent((ProtoId<ReagentPrototype>)edgeEssentia.OriginalBloodReagent, FixedPoint2.New(1));
			_bloodstream.ChangeBloodReagents((uid, bloodstream), restoreSolution);
			RemCompDeferred<EdgeEssentiaBloodComponent>(uid);
		}
		while (query.MoveNext(out uid, out edgeEssentia, out bloodstream));
	}

	private void TrackUnholyBloodLoss(EntityUid uid, EdgeEssentiaBloodComponent edgeEssentia, BloodstreamComponent bloodstream, string cultBloodReagent)
	{
		// Only count blood from player-controlled entities (those with an ACTUAL mind, not just the component)
		// This prevents farming non-sentient entities like slimes, animals, etc.
		if (!TryComp<MindContainerComponent>(uid, out var mindContainer) || mindContainer.Mind == null)
			return;

		// Check if this entity has reached the blood collection cap
		var tracker = EnsureComp<BloodCollectionTrackerComponent>(uid);
		if (tracker.TotalBloodCollected >= tracker.MaxBloodPerEntity)
			return;

		// Only track if their blood type is the cult blood reagent AND they're bleeding
		var hasCultBlood = false;
		foreach (var (reagentId, _) in bloodstream.BloodReferenceSolution.Contents)
		{
			if (reagentId.Prototype == cultBloodReagent)
			{
				hasCultBlood = true;
				break;
			}
		}
		if (!hasCultBlood || bloodstream.BleedAmount <= 0)
			return;

		// Track based on how much they're bleeding per second
		// BleedAmount represents units of blood lost per second
		// Only 1/4th of the bleed rate contributes to the ritual pool, since the bleed amount is much higher than the quantity of blood someone has
		var bloodLostThisTick = (double)(bloodstream.BleedAmount * 0.25f);
			
		// Enforce the per-entity cap. Mechanically making there no benefit to capturing people for bleeding.
		var remainingCapacity = Math.Max(0.0, tracker.MaxBloodPerEntity - tracker.TotalBloodCollected);
		var bloodToAdd = Math.Min(bloodLostThisTick, remainingCapacity);
		
		// Hard cap: never exceed the maximum
		if (bloodToAdd > 0 && tracker.TotalBloodCollected < tracker.MaxBloodPerEntity)
		{
			// Ensure we don't go over the cap even with floating point errors
			bloodToAdd = Math.Min(bloodToAdd, tracker.MaxBloodPerEntity - tracker.TotalBloodCollected);
			
			// Add to the ritual pool
			_bloodCultRule.AddBloodForConversion(bloodToAdd);
			
			// Update the tracker and clamp to max
			tracker.TotalBloodCollected = (float)Math.Min(tracker.TotalBloodCollected + bloodToAdd, tracker.MaxBloodPerEntity);
			Dirty(uid, tracker);
		}
	}

	private bool HasEdgeEssentia(EntityUid uid, BloodstreamComponent bloodstream)
	{
		// Check the chemical solution (bloodstream) for EdgeEssentia
		if (!_solutionContainer.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var chemSolution))
			return false;

		foreach (var reagent in chemSolution.Contents)
		{
			if (reagent.Reagent.Prototype == "EdgeEssentia")
				return true;
		}

		return false;
	}
}

