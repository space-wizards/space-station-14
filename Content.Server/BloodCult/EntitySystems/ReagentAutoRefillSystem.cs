// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

using Content.Server.BloodCult.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Server.BloodCult.EntitySystems;

/// <summary>
/// Handles automatic refilling of reagents in solution containers.
/// Used by cult daggers to regenerate their Edge Essentia over time.
/// Maybe a bit too fancy. But I wanted it to be more interesting than just always it inject the same amount.
/// </summary>
public sealed class ReagentAutoRefillSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        if (curTime < _nextUpdate)
            return;

        // Update every second
        _nextUpdate = curTime + TimeSpan.FromSeconds(1);

        var query = EntityQueryEnumerator<ReagentAutoRefillComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var refill, out var solutionManager))
        {
            // Try to get the solution
            if (!_solutionContainer.TryGetSolution((uid, solutionManager), refill.Solution, out var solutionEntity, out var solution))
                continue;

            // Check how much of this reagent is currently in the solution
            var currentAmount = FixedPoint2.Zero;
            foreach (var reagentQuantity in solution.Contents)
            {
                if (reagentQuantity.Reagent.Prototype == refill.Reagent)
                {
                    currentAmount = reagentQuantity.Quantity;
                    break;
                }
            }

            // Don't refill if we're at or above the max
            if (currentAmount.Float() >= refill.MaxAmount)
                continue;

            // Calculate how much to refill (1 second worth)
            var amountToAdd = FixedPoint2.New(refill.RefillRate);
            
            // Don't exceed the maximum
            var newTotal = currentAmount + amountToAdd;
            if (newTotal.Float() > refill.MaxAmount)
            {
                amountToAdd = FixedPoint2.New(refill.MaxAmount) - currentAmount;
            }

            // Add the reagent
            if (amountToAdd > 0)
            {
                _solutionContainer.TryAddReagent(solutionEntity.Value, refill.Reagent, amountToAdd, out _);
            }
        }
    }
}

