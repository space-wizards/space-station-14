using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using JetBrains.Annotations;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// "Activates" random of predefined sets of chemicals, removing them from the solution
/// and firing specified ReagentEffects once.
/// </summary>
[UsedImplicitly]
public sealed partial class ActivateRandom : EntityEffect
{
    /// <summary>
    /// List of sets of activatable chemicals along with results of the activation.
    /// </summary>
    [DataField(required: true)]
    public ActivateRandomCase[] Cases;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var guidebookCases = new List<string>();
        foreach (var caseEntry in Cases)
        {
            var guidebookActivates = new List<string>();
            foreach (var activateEntry in caseEntry.Activates)
            {
                if (prototype.TryIndex(activateEntry.Reagent.Prototype, out ReagentPrototype? reagentProto))
                    guidebookActivates.Add(
                        Loc.GetString("reagent-effect-guidebook-activate-random-activate",
                            ("factor", activateEntry.Factor),
                            ("catalyst", !activateEntry.Consume),
                            ("reagent", reagentProto.LocalizedName)));
            }

            var guidebookEffects = new List<string>();
            foreach (var effectEntry in caseEntry.Effects)
            {
                var desc = effectEntry.GuidebookEffectDescription(prototype, entSys);
                if (desc is not null)
                    guidebookEffects.Add("  " + desc);
            }

            guidebookCases.Add(
                "- "
                + string.Join(", ", guidebookActivates)
                + ":\n"
                + string.Join("\n", guidebookEffects));
        }

        return Loc.GetString(
            "reagent-effect-guidebook-activate-random",
            ("chance", Probability),
            ("cases", string.Join("\n", guidebookCases) + "\n"));
    }

    public override void Effect(EntityEffectBaseArgs baseArgs)
    {
         // ActivateRandom works by activating other reagents, it needs access to extended information
        if (baseArgs is EntityEffectReagentArgs args)
        {
            if (args.Source is null)
                return; // ActivateRandom activates chemicals from the args.Source.
                // If source is not present, ActivateRandom has nothing to activate.

            // Only one random set of chemicals is activated. The chance to activate
            // some speicific set is based on how many of these chemicals (with respect to factor)
            // are present in the solution.
            // We use chance and chanceSum to calculate proprotions of chemical sets (with respect to factor)
            // present in the solution.
            var chances = new float[Cases.Length];
            float chancesSum = 0f;

            for (int i = 0; i < Cases.Length; i++)
            {
                var potentialCase = Cases[i];
                foreach (var activate in potentialCase.Activates)
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
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            {
                float randval = robustRandom.NextFloat(0f, chancesSum);
                for (int i = 0; i < Cases.Length && randval > 0f; i++)
                {
                    index = i;
                    randval -= chances[i];
                }
            }

            // When some set is activated, ActivateRandom removes reagents of the set marked as "consumed".
            // When the effects of the set are run, the amount of *consumed* reagents as passed to them.
            // This is useful, for example, in conjuction with HealthChange's scaleByQuantity
            var consumed = FixedPoint2.Zero;
            foreach (var activate in Cases[index].Activates)
            {
                if (activate.Consume && args.Source.TryGetReagent(activate.Reagent, out var quantity))
                {
                    consumed += quantity.Quantity;
                    args.Source.RemoveReagent(quantity);
                }
            }

            // Update the quantity to the amount consumed, set reagent to null.
            var newArgs = new EntityEffectReagentArgs(
                args.TargetEntity,
                args.EntityManager,
                args.OrganEntity,
                args.Source,
                consumed, // quantity, set to the amount of reagent consumed. Therefore, catalysts only affect the chance of the
                // outcome happening, not how powerful it will be.
                null, // reagent, set to null because it's not obvious which reagent to pass when multiple reagents are consumed
                args.Method,
                args.Scale);
            foreach (var effect in Cases[index].Effects)
            {
                if (!effect.ShouldApply(newArgs, robustRandom))
                    continue;

                if (effect.ShouldLog)
                {
                    var adminLogger = IoCManager.Resolve<ISharedAdminLogManager>();
                    adminLogger.Add(
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
