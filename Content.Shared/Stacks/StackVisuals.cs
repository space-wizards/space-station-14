using Robust.Shared.Serialization;

namespace Content.Shared.Stacks
{
    [Serializable, NetSerializable]
    public enum StackVisuals : byte
    {
        /// <summary>
        /// The amount of elements in the stack
        /// </summary>
        Actual,
        /// <summary>
        /// The total amount of elements in the stack. If unspecified, the visualizer assumes
        /// it's StackComponent.LayerStates.Count
        /// </summary>
        MaxCount,
        Hide
    }
}
