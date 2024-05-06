using Content.Server.Body.Components;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    /// <summary>
    ///     Requires that the metabolizing organ is or is not tagged with a certain MetabolizerType
    /// </summary>
    public sealed partial class OrganType : ReagentEffectCondition
    {
        [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<MetabolizerTypePrototype>))]
        public string Type = default!;

        /// <summary>
        ///     Does this condition pass when the organ has the type, or when it doesn't have the type?
        /// </summary>
        [DataField]
        public bool ShouldHave = true;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.OrganEntity == null)
                return false;

            if (args.EntityManager.TryGetComponent<MetabolizerComponent>(args.OrganEntity.Value, out var metabolizer)
                && metabolizer.MetabolizerTypes != null
                && metabolizer.MetabolizerTypes.Contains(Type))
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
}
