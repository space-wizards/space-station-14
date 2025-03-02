// using Content.Server.Body.Components;
using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityEffects.EffectConditions;

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
        var evt = new CheckEntityEffectConditionEvent<OrganType> { Condition = this, Args = args };
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
        return evt.Result;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-organ-type",
            ("name", prototype.Index<MetabolizerTypePrototype>(Type).LocalizedName),
            ("shouldhave", ShouldHave));
    }
}
