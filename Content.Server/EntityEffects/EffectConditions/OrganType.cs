using Content.Server.Body.Components;
using Content.Shared.Body.Prototypes;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.EntityEffects.EffectConditions;

/// <summary>
///     Requires that the metabolizing organ is or is not tagged with a certain MetabolizerType
/// </summary>
public sealed partial class OrganType : EntityEffectCondition
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<MetabolizerTypePrototype>))]
    public string Type = default!;

    /// <summary>
    ///     Does this condition pass when the organ has the type, or when it doesn't have the type?
    /// </summary>
    [DataField]
    public bool ShouldHave = true;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            if (reagentArgs.OrganEntity == null)
                return false;

            if (reagentArgs.EntityManager.TryGetComponent<MetabolizerComponent>(reagentArgs.OrganEntity.Value, out var metabolizer)
                && metabolizer.MetabolizerTypes != null
                && metabolizer.MetabolizerTypes.Contains(Type))
                return ShouldHave;

            return !ShouldHave;
        }

        // TODO: Someone needs to figure out how to do this for non-reagent effects.
        throw new NotImplementedException();
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-organ-type",
            ("name", prototype.Index<MetabolizerTypePrototype>(Type).LocalizedName),
            ("shouldhave", ShouldHave));
    }
}
