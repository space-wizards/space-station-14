using Content.Server.Toilet;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Logic for a wrapped items.
    /// </summary>
    [RegisterComponent]
    public class WrappedStorageComponent : Component
    {
        public override string Name => "WrappedStorage";

        /// <summary>
        ///     Container used to keep item.
        /// </summary>
        [ViewVariables]
        public ContainerSlot ItemContainer = default!;

    }
}
