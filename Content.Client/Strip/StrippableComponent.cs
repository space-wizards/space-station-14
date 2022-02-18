using Content.Shared.DragDrop;
using Content.Shared.Strip.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Strip
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrippableComponent))]
    public sealed class StrippableComponent : SharedStrippableComponent
    {
        public override bool Drop(DragDropEvent args)
        {
            // TODO: Prediction
            return false;
        }
    }
}
