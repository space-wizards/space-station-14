using System.Diagnostics.CodeAnalysis;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public class MaterialConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        // TODO: Make this use the material system.
        // TODO TODO: Make the material system not shit.
        [DataField("material")] public string MaterialPrototypeId { get; } = "Steel";

        [DataField("amount")] public int Amount { get; } = 1;

        public StackPrototype MaterialPrototype =>
            IoCManager.Resolve<IPrototypeManager>().Index<StackPrototype>(MaterialPrototypeId);

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            examinedEvent.Message.AddMarkup(Loc.GetString("construction-insert-material-entity", ("amount", Amount), ("materialName", MaterialPrototype.Name)));
        }

        public override bool EntityValid(IEntity entity)
        {
            return entity.TryGetComponent(out SharedStackComponent? stack) && stack.StackTypeId.Equals(MaterialPrototypeId) && stack.Count >= Amount;
        }

        public bool EntityValid(IEntity entity, [NotNullWhen(true)] out SharedStackComponent? stack)
        {
            if (entity.TryGetComponent(out SharedStackComponent? otherStack) && otherStack.StackTypeId.Equals(MaterialPrototypeId) && otherStack.Count >= Amount)
                stack = otherStack;
            else
                stack = null;

            return stack != null;
        }
    }
}
