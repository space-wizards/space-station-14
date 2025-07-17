using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.EffectConditions;

/// <summary>
/// A condition which passes if the specified <see cref="SatiationType"/> is between the specified <see cref="Min"/> and
/// <see cref="Max"/>. If the entity does not have the specified satiation, the condition evaluates to false.
/// </summary>
public sealed partial class Satiation : EntityEffectCondition
{
    /// <summary>
    /// The value above which this condition will fail. If <see cref="MaxInclusive"/> is false, the condition will fail
    /// if at that value as well.
    /// </summary>
    [DataField]
    public float Max = float.PositiveInfinity;

    /// <summary>
    /// The value below which this condition will fail. If <see cref="MinInclusive"/> is false, the condition will fail
    /// if at that value as well.
    /// </summary>
    [DataField]
    public float Min = 0;

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Max"/> will NOT fail.
    /// </summary>
    [DataField]
    public bool MaxInclusive = false;

    /// <summary>
    /// If <c>true</c>, values exactly equal to <see cref="Min"/> will NOT fail.
    /// </summary>
    [DataField]
    public bool MinInclusive = false;

    /// <summary>
    /// The type of satiation whose value will be considered.
    /// </summary>
    [DataField]
    public ProtoId<SatiationTypePrototype> SatiationType;

    [Dependency] private readonly SatiationSystem _satiation = default!;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        // Access the satiation component on the entity, then the specific `SatiationType` on the component.
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out SatiationComponent? satiationComp) ||
            _satiation.GetValueOrNull((args.TargetEntity, satiationComp), SatiationType) is not { } satiation)
        {
            return false;
        }

        return (MinInclusive && satiation >= Min || satiation > Min) &&
                  (MaxInclusive && satiation <= Max || satiation < Max);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-total-satiation",
            ("max", float.IsPositiveInfinity(Max) ? int.MaxValue : Max),
            ("min", Min),
            ("type", Loc.GetString(prototype.Index(SatiationType).Name)));
    }
}
