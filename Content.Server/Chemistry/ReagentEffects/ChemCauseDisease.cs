using Content.Shared.Chemistry.Reagent;
using Content.Server.Disease;
using Content.Shared.Disease;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemCauseDisease : ReagentEffect
    {
        /// <summary>
        /// Chance it has each tick to cause disease, between 0 and 1
        /// </summary>
        [DataField("causeChance")]
        public float CauseChance = 0.15f;

        /// <summary>
        /// The disease to add.
        /// </summary>
        [DataField("disease", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>), required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Disease = default!;

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.MetabolismEffects != null && args.Quantity < args.MetabolismEffects.MetabolismRate)
                return; // the metabolism rate is the minimum needed to add a disease.

            EntitySystem.Get<DiseaseSystem>().TryAddDisease(args.SolutionEntity, Disease);
        }
    }
}
