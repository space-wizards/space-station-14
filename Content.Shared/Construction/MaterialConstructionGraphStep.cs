#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class MaterialConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        // TODO: Make this use the material system.
        // TODO TODO: Make the material system not shit.
        private string MaterialPrototypeId { get; [UsedImplicitly] set; } = default!;

        public StackPrototype MaterialPrototype =>
            IoCManager.Resolve<IPrototypeManager>().Index<StackPrototype>(MaterialPrototypeId);

        public int Amount { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.MaterialPrototypeId, "material", "Steel");
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }

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
