using System.Numerics;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared.Body.Systems;

public sealed partial class BloodstreamSystem
{
    [Dependency] private ThrowingSystem _throwing = default!;

    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    [SubscribeLocalEvent]
    private void OnDamage(Entity<BloodstreamDripOnDamageComponent> ent, ref DamageDealtEvent args)
    {
        if (!_bloodstreamQuery.TryComp(ent, out var bloodstream))
            return;

        if (!_solutionContainer.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution))
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        if (!rand.Prob(ent.Comp.Probability))
            return;

        foreach (var damage in args.Damage.DamageDict)
        {
            if (!ent.Comp.Allowed.Contains(damage.Key))
                continue;

            if (ent.Comp.Threshold > damage.Value)
                continue;

            var (min, max) = ent.Comp.Amount;
            for (var i = 0; i <= rand.Next(min, max); i++)
            {
                if (!TrySpawnDroplet((ent, bloodstream),
                    rand.NextVector2() * ent.Comp.Range.NextFloat(rand),
                    ent.Comp.Force.NextFloat(rand)))
                    return;
            }

            return;
        }
    }

    /// <summary>
    /// Updates the amount of blood needed for a blood droplet transfer.
    /// </summary>
    /// <param name="ent">The entity to update for.</param>
    [PublicAPI]
    public void UpdateBloodDropletTransferAmount(Entity<BloodstreamComponent?> ent)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new ModifyBloodDropletEvent(ent.Comp.BasicDropletTransferAmount);
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.DropletTransferAmount = FixedPoint2.Max(ev.BloodAmount, 0f);
        DirtyField(ent, nameof(BloodstreamComponent.DropletTransferAmount));
    }

    /// <summary>
    /// Spawns a blood droplet and throws it.
    /// </summary>
    /// <param name="ent">The entity from which to spawn the blood droplet.</param>
    /// <param name="dir">The direction in which the blood droplet will fly.</param>
    /// <param name="force">The force with which the blood droplet will fly.</param>
    [PublicAPI]
    public bool TrySpawnDroplet(Entity<BloodstreamComponent?> ent, Vector2 dir, float force)
    {
        if (!_bloodstreamQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution))
            return false;

        if (ent.Comp.DropletTransferAmount == 0f)
            return false;

        if (ent.Comp.BloodSolution.Value.Comp.Solution.Volume < ent.Comp.DropletTransferAmount)
            return false;

        var droplet = PredictedSpawnAtPosition(BloodstreamComponent.DropletId, Transform(ent).Coordinates);

        if (_solutionContainer.TryGetSolution(droplet, BloodstreamComponent.DropletSolution, out var solution, true))
        {
            solution.Value.Comp.Solution.RemoveAllSolution();

            var amount = _solutionContainer.SplitSolution(
                ent.Comp.BloodSolution.Value,
                FixedPoint2.Min(ent.Comp.DropletTransferAmount, solution.Value.Comp.Solution.AvailableVolume));

            _solutionContainer.TryAddSolution(solution.Value, amount);
        }

        _throwing.TryThrow(droplet, dir, force, compensateFriction: true);

        return true;
    }
}

/// <summary>
/// Raised to allow other systems to modify the amount of blood transferred to the blood droplet.
/// </summary>
/// <param name="BloodAmount">The default amount of blood to modify.</param>
[ByRefEvent]
public record struct ModifyBloodDropletEvent(FixedPoint2 BloodAmount);
