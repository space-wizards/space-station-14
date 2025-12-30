using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class AdjustReagentGaussian : EntityEffectBase<AdjustReagentGaussian>
{
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    [DataField(required: true)]
    public double μ;

    [DataField(required: true)]
    public double σ;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var proto = prototype.Index(Reagent);
        return Loc.GetString("entity-effect-guidebook-adjust-reagent-gaussian",
            ("chance", Probability),
            ("deltasign", Math.Sign(μ)),
            ("reagent", proto.LocalizedName),
            ("mu", Math.Abs(μ)),
            ("sigma", Math.Abs(σ)));
    }
}

public sealed class AdjustReagentGaussianEntityEffectSystem : EntityEffectSystem<SolutionComponent, AdjustReagentGaussian>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void Effect(Entity<SolutionComponent> ent, ref EntityEffectEvent<AdjustReagentGaussian> args)
    {
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var amount = rand.NextGaussian(args.Effect.μ, args.Effect.σ);
        amount *= args.Scale;
        var reagent = args.Effect.Reagent;

        if (amount < 0)
            _solutionContainer.RemoveReagent(ent, reagent, -amount);
        else if (amount > 0)
            _solutionContainer.TryAddReagent(ent, reagent, amount);
    }
}
