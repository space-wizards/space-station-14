using Content.Server.Storage.EntitySystems;
using Content.Server.Toilet;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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
        ///     Max item size that can be fitted into secret stash.
        /// </summary>
        [ViewVariables] [DataField("maxItemSize")]
        public int MaxItemSize = (int) ReferenceSizes.Pocket;

        /// <summary>
        ///     Container used to keep item.
        /// </summary>
        [ViewVariables]
        public ContainerSlot ItemContainer = default!;

    }
}
