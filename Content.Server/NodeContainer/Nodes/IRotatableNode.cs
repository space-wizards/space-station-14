using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.NodeContainer.Nodes
{
    /// <summary>
    ///     A <see cref="Node"/> that implements this will have its <see cref="RotateEvent(RotateEvent)"/> called when its
    ///     <see cref="NodeContainerComponent"/> is rotated.
    /// </summary>
    public interface IRotatableNode
    {
        /// <summary>
        ///     Rotates this <see cref="Node"/>.
        /// </summary>
        void RotateEvent(RotateEvent ev);
    }
}
