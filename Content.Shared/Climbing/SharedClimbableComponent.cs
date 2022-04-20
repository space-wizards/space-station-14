using Content.Shared.DragDrop;
using Content.Shared.Interaction;

namespace Content.Shared.Climbing
{
    public abstract class SharedClimbableComponent : Component
    {
        /// <summary>
        ///     The range from which this entity can be climbed.
        /// </summary>
        [ViewVariables] [DataField("range")] public float Range = SharedInteractionSystem.InteractionRange / 1.4f;
    }
}
