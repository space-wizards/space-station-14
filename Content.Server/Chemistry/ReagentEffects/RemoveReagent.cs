using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public class RemoveReagent : ReagentEffect
    {
        [DataField("reagent", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string Reagent = default!;

        [DataField("amount", required: true)]
        public FixedPoint2 Amount = default!;

        public override void Metabolize(ReagentEffectArgs args)
        {
            if (args.Source != null && args.Source.ContainsReagent(Reagent))
            {
                var solutionSys = args.EntityManager.EntitySysManager.GetEntitySystem<SolutionContainerSystem>();
                solutionSys.TryRemoveReagent(args.SolutionEntity, args.Source, Reagent, Amount);
            }
        }
    }
}
