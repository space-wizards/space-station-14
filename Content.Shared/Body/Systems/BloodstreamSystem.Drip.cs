using System.Numerics;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Body.Systems;

public sealed partial class BloodstreamSystem
{
    [Dependency] private ThrowingSystem _throwing = default!;

    [Dependency] private EntityQuery<BloodstreamComponent> _bloodstreamQuery = default!;

    /// <summary>
    /// The blood droplet entity id.
    /// </summary>
    private static readonly EntProtoId DropletId = "Droplet";

    /// <summary>
    /// The amount of blood that will be transferred to the blood droplet.
    /// </summary>
    private static readonly FixedPoint2 DropletTransferAmount = 1f;

    /// <summary>
    /// The blood droplet solution to which blood will be added.
    /// </summary>
    private const string DropletSolution = "solution";

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
            var (minRange, maxRange) = ent.Comp.Range;
            var (minForce, maxForce) = ent.Comp.Force;
            for (var i = 0; i <= rand.Next(min, max); i++)
            {
                if (!SpawnDroplet((ent, bloodstream), rand.NextVector2() * rand.NextFloat(minRange, maxRange), rand.NextFloat(minForce, maxForce)))
                    return;
            }

            return;
        }
    }

    /// <summary>
    /// Spawns a blood droplet and throws it.
    /// </summary>
    /// <param name="ent">The entity from which to spawn the blood droplet.</param>
    /// <param name="dir">The direction in which the blood droplet will fly.</param>
    /// <param name="force">The force with which the blood droplet will fly.</param>
    [PublicAPI]
    public bool SpawnDroplet(Entity<BloodstreamComponent?> ent, Vector2 dir, float force)
    {
        if (!_bloodstreamQuery.TryComp(ent, out var bloodstream))
            return false;

        if (!_solutionContainer.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution))
            return false;

        if (bloodstream.BloodSolution.Value.Comp.Solution.Volume <= DropletTransferAmount)
            return false;

        var droplet = PredictedSpawnAtPosition(DropletId, Transform(ent).Coordinates);

        if (_solutionContainer.TryGetSolution(droplet, DropletSolution, out var solution, true))
        {
            solution.Value.Comp.Solution.RemoveAllSolution();

            var amount = _solutionContainer.SplitSolution(bloodstream.BloodSolution.Value, DropletTransferAmount);
            _solutionContainer.TryAddSolution(solution.Value, amount);
        }

        _throwing.TryThrow(droplet, dir, force, compensateFriction: true);

        return true;
    }
}
