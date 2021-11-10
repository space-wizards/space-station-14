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

        public override void Metabolize(EntityUid solutionEntity, EntityUid organEntity, Solution.ReagentQuantity reagent, IEntityManager entityManager)
        {
            // implement later lol its harder than i thought
        }
    }
}
