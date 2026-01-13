// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Server.Forensics;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.BloodCult.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.BloodCult;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Timing;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// System that allows mops to clean blood cult runes.
/// This extends AbsorbentSystem behavior without modifying the core system.
/// </summary>
public sealed class BloodCultRuneCleaningSystem : EntitySystem
{
	[Dependency] private readonly ForensicsSystem _forensics = default!;
	[Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
	[Dependency] private readonly UseDelaySystem _useDelay = default!;

	public override void Initialize()
	{
		base.Initialize();
		
		// Subscribe to the custom event raised by AbsorbentSystem when a mop is used
		// This allows us to handle rune cleaning before AbsorbentSystem processes puddles/refillables
		SubscribeLocalEvent<CleanableRuneComponent, AbsorbentMopTargetEvent>(OnAbsorbentMopTarget);
	}

	private void OnAbsorbentMopTarget(Entity<CleanableRuneComponent> entity, ref AbsorbentMopTargetEvent args)
	{
		if (TryCleanRuneWithMop(args.User, entity.Owner, args.Used, args.Component))
			args.Handled = true;
	}

	/// <summary>
	/// Attempts to clean a rune with a mop. Returns true if the rune was cleaned (or cleaning started).
	/// </summary>
	private bool TryCleanRuneWithMop(EntityUid user, EntityUid target, EntityUid mop, AbsorbentComponent component)
	{

		// Check if mop has a solution
		if (!_solutionContainer.TryGetSolution(mop, component.SolutionName, out var absorberSoln))
			return false;

		// Check if mop is on cooldown
		if (TryComp<UseDelayComponent>(mop, out var useDelay) && _useDelay.IsDelayed((mop, useDelay)))
			return false;

		var solution = absorberSoln.Value.Comp.Solution;
		
		// Check if mop has water or space cleaner by iterating through solution contents
		bool hasWater = false;
		bool hasSpaceCleaner = false;
		foreach (var (reagentId, quantity) in solution.Contents)
		{
			if (reagentId.Prototype == "Water" && quantity > FixedPoint2.Zero)
				hasWater = true;
			if (reagentId.Prototype == "SpaceCleaner" && quantity > FixedPoint2.Zero)
				hasSpaceCleaner = true;
		}

		if (!hasWater && !hasSpaceCleaner)
			return false;

		// Use the forensics system to clean the rune
		// Create a temporary CleansForensics component for the mop
		var cleansForensics = EnsureComp<CleansForensicsComponent>(mop);
		cleansForensics.CleanDelay = 3f; // Mops are slower than soap
		
		return _forensics.TryStartCleaning((mop, cleansForensics), user, target);
	}
}

