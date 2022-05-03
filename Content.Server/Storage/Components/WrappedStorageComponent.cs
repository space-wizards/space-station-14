using Content.Server.Toilet;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Logic for a wrapped items.
    ///     Unlike <see cref="ItemSlotsComponent"/> it doesn't have interaction logic or verbs.
    ///     Other classes like <see cref="ToiletComponent"/> should implement it.
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
