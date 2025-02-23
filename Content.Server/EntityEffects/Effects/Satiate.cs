using Content.Shared.EntityEffects;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Attempts to find a <see cref="SatiationComponent"/> on the target, and update its value of the specified
/// <see cref="SatiationType"/>.
/// </summary>
public sealed partial class Satiate : EntityEffect
{
    private const float DefaultSatiationFactor = 3;

    /// <summary>
    /// How much the specified <see cref="SatiationType"/> is satiated. Is multiplied by quantity if used with
    /// EntityEffectReagentArgs.
    /// </summary>
    [DataField]
    public float Factor { get; set; } = DefaultSatiationFactor;

    /// <summary>
    /// The type of satiation to modify.
    /// </summary>
    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out SatiationComponent? comp))
        {
            return;
        }

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            args.EntityManager.System<SatiationSystem>()
                .ModifyValue((args.TargetEntity, comp), SatiationType, Factor * (float)reagentArgs.Quantity);
        }
        else
        {
            args.EntityManager.System<SatiationSystem>()
                .ModifyValue((args.TargetEntity, comp), SatiationType, Factor);
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-satiate",
            ("chance", Probability),
            ("relative", Factor / DefaultSatiationFactor),
            ("type", Loc.GetString(prototype.Index(SatiationType).Name)));
    }
}
