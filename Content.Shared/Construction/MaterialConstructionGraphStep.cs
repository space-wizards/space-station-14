#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
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

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Next, add [color=yellow]{0}x[/color] [color=cyan]{1}[/color].", Amount, MaterialPrototype.Name));
        }

        public override bool EntityValid(IEntity entity)
        {
            return entity.TryGetComponent(out SharedStackComponent? stack) && stack.StackTypeId.Equals(MaterialPrototypeId);
        }

        public bool EntityValid(IEntity entity, [NotNullWhen(true)] out SharedStackComponent? stack)
        {
            if (entity.TryGetComponent(out SharedStackComponent? otherStack) && otherStack.StackTypeId.Equals(MaterialPrototypeId))
                stack = otherStack;
            else
                stack = null;

            return stack != null;
        }
    }
}
