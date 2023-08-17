using System.Diagnostics.CodeAnalysis;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class MaterialConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        // TODO: Make this use the material system.
        // TODO TODO: Make the material system not shit.
        [DataField("material", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        public string MaterialPrototypeId { get; private set; } = "Steel";

        [DataField("amount")] public int Amount { get; private set; } = 1;

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            var material = IoCManager.Resolve<IPrototypeManager>().Index<StackPrototype>(MaterialPrototypeId);

            examinedEvent.Message.AddMarkup(Loc.GetString("construction-insert-material-entity", ("amount", Amount), ("materialName", material.Name)));
        }

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            return entityManager.TryGetComponent(uid, out StackComponent? stack) && stack.StackTypeId == MaterialPrototypeId && stack.Count >= Amount;
        }

        public bool EntityValid(EntityUid entity, [NotNullWhen(true)] out StackComponent? stack)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out StackComponent? otherStack) && otherStack.StackTypeId == MaterialPrototypeId && otherStack.Count >= Amount)
                stack = otherStack;
            else
                stack = null;

            return stack != null;
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var material = IoCManager.Resolve<IPrototypeManager>().Index<StackPrototype>(MaterialPrototypeId);

            return new ConstructionGuideEntry()
            {
                Localization = "construction-presenter-material-step",
                Arguments = new (string, object)[]{("amount", Amount), ("material", material.Name)},
                Icon = material.Icon,
            };
        }
    }
}
