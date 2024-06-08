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

            return Condition(args.OrganEntity.Value, args.EntityManager);
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
}
