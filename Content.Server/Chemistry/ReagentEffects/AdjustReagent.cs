using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed class AdjustReagent : ReagentEffect
    {
        /// <summary>
        ///     The reagent ID to remove. Only one of this and <see cref="Group"/> should be active.
        /// </summary>
        [DataField("reagent", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string? Reagent = null;

        /// <summary>
        ///     The metabolism group to remove, if the reagent satisfies any.
        ///     Only one of this and <see cref="Reagent"/> should be active.
        /// </summary>
        [DataField("group", customTypeSerializer:typeof(PrototypeIdSerializer<MetabolismGroupPrototype>))]
        public string? Group = null;

        [DataField("amount", required: true)]
        public FixedPoint2 Amount = default!;

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Source != null)
            {
                var solutionSys = args.EntityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
                if (Reagent != null)
                {
                    if (Amount < 0 && args.Source.ContainsReagent(Reagent))
                        solutionSys.TryRemoveReagent(args.SolutionEntity, args.Source, Reagent, -Amount);
                    if (Amount > 0)
                        solutionSys.TryAddReagent(args.SolutionEntity, args.Source, Reagent, Amount, out _);
                }
                else if (Group != null)
                {
                    var prototypeMan = IoCManager.Resolve<IPrototypeManager>();
                    foreach (var quant in args.Source.Contents.ToArray())
                    {
                        var proto = prototypeMan.Index<ReagentPrototype>(quant.ReagentId);
                        if (proto.Metabolisms != null && proto.Metabolisms.ContainsKey(Group))
                        {
                            if (Amount < 0)
                                solutionSys.TryRemoveReagent(args.SolutionEntity, args.Source, quant.ReagentId, Amount);
                            if (Amount > 0)
                                solutionSys.TryAddReagent(args.SolutionEntity, args.Source, quant.ReagentId, Amount, out _);
                        }
                    }
                }
            }
        }
    }
}
