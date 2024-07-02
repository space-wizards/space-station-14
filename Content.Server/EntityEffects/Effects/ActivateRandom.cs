using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using JetBrains.Annotations;
using System.Linq;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// "Activates" random of predefined sets of chemicals, removing them from the solution
/// and firing specified ReagentEffects once.
/// </summary>
[UsedImplicitly]
public sealed partial class ActivateRandom : EntityEffect {
    /// <summary>
    /// List of sets of activatable chemicals along with results of the activation.
    /// </summary>
    [DataField(required: true)]
    public ActivateRandomCase[] Cases;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-activate-random",
            ("chance", Probability),
            ("cases", string.Join("\n", Cases.ToList().Select(case_ =>
                "- "
                + string.Join(", ", case_.Activates.ToList()
                    .Select(activate =>
                        prototype.TryIndex(activate.Reagent.Prototype, out ReagentPrototype? reagentProto)
                            ? Loc.GetString("reagent-effect-guidebook-activate-random-activate",
                                ("factor", activate.Factor),
                                ("catalyst", !activate.Consume),
                                ("reagent", reagentProto.LocalizedName))
                            : null)
                        .Where(x => x is not null))
                + ":\n"
                + string.Join("\n", case_.Effects.ToList()
                    .Select(effect =>
                    {
                        var desc = effect.GuidebookEffectDescription(prototype, entSys);
                        return desc is null ? null : "  " + desc;
                    })
                    .Where(x => x is not null))))
                + "\n"));
    }

    public override void Effect(EntityEffectBaseArgs baseArgs)
    {
         // ActivateRandom works by activating other reagents, it needs access to extended information
        if (baseArgs is not EntityEffectReagentArgs)
            return;
        EntityEffectReagentArgs args = (EntityEffectReagentArgs) baseArgs;
            
        if (args.Source is null)
            return; // ActivateRandom activates chemicals from the args.Source.
            // If source is not present, ActivateRandom has nothing to activate.

        // Only one random set of chemicals is activated. The chance to activate
        // some speicific set is based on how many of these chemicals (with respect to factor)
        // are present in the solution.
        // We use chance and chanceSum to calculate proprotions of chemical sets (with respect to factor)
        // present in the solution.
        float[] chances = new float[Cases.Length];
        float chancesSum = 0f;

        for (int i = 0; i < Cases.Length; i++)
        {
            var _case = Cases[i];
            foreach (var activate in _case.Activates)
            {
                if (args.Source.TryGetReagent(activate.Reagent, out var quantity))
                {
                    var impact = quantity.Quantity.Float() * activate.Factor.Float();
                    chances[i] += impact;
                    chancesSum += impact;
                }
            }
        }

        // Next, select a "random" set. Sets of higher propotions are prioritized.
        int index = 0;
        IRobustRandom _random = IoCManager.Resolve<IRobustRandom>();
        {
            float randval = _random.NextFloat(0f, chancesSum);
            for (int i = 0; i < Cases.Length && randval > 0f; i++) {
                index = i;
                randval -= chances[i];
            }
        }

        // When some set is activated, ActivateRandom removes reagents of the set marked as "consumed".
        // When the effects of the set are run, the amount of *consumed* reagents as passed to them.
        // This is useful, for example, in conjuction with HealthChange's scaleByQuantity
        FixedPoint2 consumed = 0f;
        foreach (var activate in Cases[index].Activates) {
            if (activate.Consume && args.Source.TryGetReagent(activate.Reagent, out var quantity)) {
                consumed += quantity.Quantity;
                args.Source.RemoveReagent(quantity);
            }
        }

        // Update the quantity to the amount consumed, set reagent to null.
        EntityEffectReagentArgs newArgs = new EntityEffectReagentArgs(
            args.TargetEntity,
            args.EntityManager,
            args.OrganEntity,
            args.Source,
            consumed, // quantity, set to the amount of reagent consumed. Therefore, catalysts only affect the chance of the
            // outcome happening, not how powerful it will be.
            null, // reagent, set to null because it's not obvious which reagent to pass when multiple reagents are consumed
            args.Method,
            args.Scale);
        foreach (var effect in Cases[index].Effects) {
            if (!effect.ShouldApply(newArgs, _random))
                continue;

            if (effect.ShouldLog)
            {
                var _adminLogger = IoCManager.Resolve<ISharedAdminLogManager>();
                _adminLogger.Add(
                    LogType.ReagentEffect,
                    effect.LogImpact,
                    $"Metabolism effect {effect.GetType().Name:effect}"
                    + $" of reagent {args.Reagent?.LocalizedName:reagent}"
                    + $" applied on entity {args.TargetEntity:entity}"
                    + $" at {args.EntityManager.GetComponent<TransformComponent>(args.TargetEntity).Coordinates:coordinates}"
                );
            }
            effect.Effect(newArgs);
        }
    }
}

/// <summary>
/// One of the cases. Lists activatable chemicals and effects of their activation.
/// </summary>
[DataDefinition]
public sealed partial class ActivateRandomCase
{
    [DataField(required: true)]
    public ActivateRandomCaseActivate[] Activates;

    [DataField(required: true)]
    public EntityEffect[] Effects;
}

/// <summary>
/// List of chemicals that constitute the activatable set.
/// Probably usually consists of one consumed reagent and zero or multiple "catalysts".
/// </summary>
[DataDefinition]
public sealed partial class ActivateRandomCaseActivate
{
    [DataField(required: true)]
    public ReagentId Reagent;

    [DataField(required: true)]
    public bool Consume;

    [DataField]
    public FixedPoint2 Factor = FixedPoint2.New(1);
}
