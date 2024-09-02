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

            return Condition(reagentArgs.OrganEntity.Value, reagentArgs.EntityManager);
        }

        // TODO: Someone needs to figure out how to do this for non-reagent effects.
        throw new NotImplementedException();
    }

    public bool Condition(Entity<MetabolizerComponent?> metabolizer, IEntityManager entMan)
    {
        metabolizer.Comp ??= entMan.GetComponentOrNull<MetabolizerComponent>(metabolizer.Owner);
        if (metabolizer.Comp != null
            && metabolizer.Comp.MetabolizerTypes != null
            && metabolizer.Comp.MetabolizerTypes.Contains(Type))
            return ShouldHave;
        return !ShouldHave;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-organ-type",
            ("name", prototype.Index<MetabolizerTypePrototype>(Type).LocalizedName),
            ("shouldhave", ShouldHave));
    }
}
