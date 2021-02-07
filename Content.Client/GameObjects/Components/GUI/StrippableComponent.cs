using Content.Shared.GameObjects.Components.GUI;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.GUI
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrippableComponent))]
    public class StrippableComponent : SharedStrippableComponent
    {
        public override bool Drop(DragDropEventArgs args)
        {
            // TODO: Prediction
            return false;
        }
    }
}
