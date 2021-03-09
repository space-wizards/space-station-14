#nullable enable
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.GUI
{
    /// <summary>
    ///     Give to an entity to say they can strip another entity.
    /// </summary>
    [RegisterComponent]
    public class SharedStrippingComponent : Component, IDragDropOn
    {
        public override string Name => "Stripping";

        bool IDragDropOn.CanDragDropOn(DragDropEventArgs eventArgs)
        {
            if (!eventArgs.Dragged.TryGetComponent(out SharedStrippableComponent? strippable)) return false;
            return strippable.CanBeStripped(Owner);
        }

        bool IDragDropOn.DragDropOn(DragDropEventArgs eventArgs)
        {
            // Handled by StrippableComponent
            return true;
        }
    }
}
