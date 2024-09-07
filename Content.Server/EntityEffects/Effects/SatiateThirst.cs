using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Default metabolism for drink reagents. Attempts to find a ThirstComponent on the target,
/// and to update it's thirst values.
/// </summary>
public sealed partial class SatiateThirst : EntityEffect
{
    private const float DefaultHydrationFactor = 3.0f;

    /// How much thirst is satiated each tick. Not currently tied to
    /// rate or anything.
    [DataField("factor")]
    public float HydrationFactor { get; set; } = DefaultHydrationFactor;

    /// Satiate thirst if a ThirstComponent can be found
    public override void Effect(EntityEffectBaseArgs args)
    {
        var uid = args.TargetEntity;
        if (args.EntityManager.TryGetComponent(uid, out ThirstComponent? thirst))
            args.EntityManager.System<ThirstSystem>().ModifyThirst(uid, thirst, HydrationFactor);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-satiate-thirst", ("chance", Probability), ("relative",  HydrationFactor / DefaultHydrationFactor));
}
