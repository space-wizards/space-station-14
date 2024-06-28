using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[ImplicitDataDefinitionForInheritors]
public abstract partial class PlantAdjustAttribute : EntityEffect
{
    [DataField]
    public float Amount { get; protected set; } = 1;

    /// <summary>
    /// Localisation key for the name of the adjusted attribute. Used for guidebook descriptions.
    /// </summary>
    [DataField]
    public abstract string GuidebookAttributeName { get; set; }

    /// <summary>
    /// Whether the attribute in question is a good thing. Used for guidebook descriptions to determine the color of the number.
    /// </summary>
    [DataField]
    public virtual bool GuidebookIsAttributePositive { get; protected set; } = true;

    /// <summary>
    ///     Checks if the plant holder can metabolize the reagent or not. Checks if it has an alive plant by default.
    /// </summary>
    /// <param name="plantHolder">The entity holding the plant</param>
    /// <param name="plantHolderComponent">The plant holder component</param>
    /// <param name="entityManager">The entity manager</param>
    /// <param name="mustHaveAlivePlant">Whether to check if it has an alive plant or not</param>
    /// <returns></returns>
    public bool CanMetabolize(EntityUid plantHolder, [NotNullWhen(true)] out PlantHolderComponent? plantHolderComponent,
        IEntityManager entityManager,
        bool mustHaveAlivePlant = true)
    {
        plantHolderComponent = null;

        if (!entityManager.TryGetComponent(plantHolder, out plantHolderComponent)
                                || mustHaveAlivePlant && (plantHolderComponent.Seed == null || plantHolderComponent.Dead))
            return false;

        return true;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        string color;
        if (GuidebookIsAttributePositive ^ Amount < 0.0)
        {
            color = "green";
        }
        else
        {
            color = "red";
        }
        return Loc.GetString("reagent-effect-guidebook-plant-attribute", ("attribute", Loc.GetString(GuidebookAttributeName)), ("amount", Amount.ToString("0.00")), ("colorName", color), ("chance", Probability));
    }
}
