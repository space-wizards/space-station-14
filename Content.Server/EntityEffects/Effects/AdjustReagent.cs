using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class AdjustReagent : EntityEffect
    {
        /// <summary>
        ///     The reagent ID to remove. Only one of this and <see cref="Group"/> should be active.
        /// </summary>
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string? Reagent = null;
        // TODO use ReagentId

        /// <summary>
        ///     The metabolism group to remove, if the reagent satisfies any.
        ///     Only one of this and <see cref="Reagent"/> should be active.
        /// </summary>
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<MetabolismGroupPrototype>))]
        public string? Group = null;

        [DataField(required: true)]
        public FixedPoint2 Amount = default!;

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (args is EntityEffectReagentArgs reagentArgs)
            {
                if (reagentArgs.Source == null)
                    return;

                var amount = Amount;
                amount *= reagentArgs.Scale;

                if (Reagent != null)
                {
                    if (amount < 0 && reagentArgs.Source.ContainsPrototype(Reagent))
                        reagentArgs.Source.RemoveReagent(Reagent, -amount);
                    if (amount > 0)
                        reagentArgs.Source.AddReagent(Reagent, amount);
                }
                else if (Group != null)
                {
                    var chemRegistry = IoCManager.Resolve<IEntityManager>().System<ChemistryRegistrySystem>();
                    foreach (var quant in reagentArgs.Source.Contents.ToArray())
                    {
                        var reagentDef = chemRegistry.Index(quant.Reagent.Prototype);
                        if (reagentDef.Comp.Metabolisms != null && reagentDef.Comp.Metabolisms.ContainsKey(Group))
                        {
                            if (amount < 0)
                                reagentArgs.Source.RemoveReagent(quant.Reagent, amount);
                            if (amount > 0)
                                reagentArgs.Source.AddReagent(quant.Reagent, amount);
                        }
                    }
                }
                return;
            }

            // TODO: Someone needs to figure out how to do this for non-reagent effects.
            throw new NotImplementedException();
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            if (Reagent is not null && entSys.GetEntitySystem<ChemistryRegistrySystem>().TryIndex(Reagent, out var reagentDef))
            {
                return Loc.GetString("reagent-effect-guidebook-adjust-reagent-reagent",
                    ("chance", Probability),
                    ("deltasign", MathF.Sign(Amount.Float())),
                    ("reagent", reagentDef.Comp.LocalizedName),
                    ("amount", MathF.Abs(Amount.Float())));
            }
            else if (Group is not null && prototype.TryIndex(Group, out MetabolismGroupPrototype? groupProto))
            {
                return Loc.GetString("reagent-effect-guidebook-adjust-reagent-group",
                    ("chance", Probability),
                    ("deltasign", MathF.Sign(Amount.Float())),
                    ("group", groupProto.LocalizedName),
                    ("amount", MathF.Abs(Amount.Float())));
            }

            throw new NotImplementedException();
        }
    }
}
