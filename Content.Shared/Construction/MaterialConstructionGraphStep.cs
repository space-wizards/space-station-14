#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Materials;
using Content.Shared.Materials;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public class MaterialConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        // TODO: Make this use the material system.
        // TODO TODO: Make the material system not shit.
        public StackType Material { get; private set; }
        public int Amount { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Material, "material", StackType.Metal);
            serializer.DataField(this, x => x.Amount, "amount", 1);
        }

        public override void DoExamine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Next, add [color=yellow]{0}x[/color] [color=cyan]{1}[/color].", Amount, Material));
        }

        public override bool EntityValid(IEntity entity)
        {
            return entity.TryGetComponent(out SharedStackComponent? stack) && stack.StackType.Equals(Material);
        }

        public bool EntityValid(IEntity entity, [NotNullWhen(true)] out SharedStackComponent? stack)
        {
            if(entity.TryGetComponent(out SharedStackComponent? otherStack) && otherStack.StackType.Equals(Material))
                stack = otherStack;
            else
                stack = null;

            return stack != null;
        }
    }
}
