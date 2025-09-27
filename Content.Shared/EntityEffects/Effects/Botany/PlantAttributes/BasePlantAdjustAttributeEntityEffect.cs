using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public abstract partial class BasePlantAdjustAttribute<T> : EntityEffectBase<T> where T : BasePlantAdjustAttribute<T>
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

    // TODO: For guidebook might want to use this tbqh...
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-attribute",
        ("attribute", Loc.GetString(GuidebookAttributeName)),
        ("amount", Amount.ToString("0.00")),
        ("positive", GuidebookIsAttributePositive),
        ("chance", Probability));
}
