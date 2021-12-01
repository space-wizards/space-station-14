using Content.Server.Toilet;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Logic for a secret slot stash, like plant pot or toilet cistern.
    ///     Unlike <see cref="ItemSlotsComponent"/> it doesn't have interaction logic.
    ///     Other classes like <see cref="ToiletComponent"/> should implement it.
    /// </summary>
    [RegisterComponent]
    public class SecretStashComponent : Component
    {
        public override string Name => "SecretStash";

        /// <summary>
        ///     Max item size that can be fitted into secret stash.
        /// </summary>
        [ViewVariables] [DataField("maxItemSize")]
        public int MaxItemSize = (int) ReferenceSizes.Pocket;

        /// <summary>
        ///     IC secret stash name. For example "the toilet cistern".
        /// </summary>
        [ViewVariables] [DataField("secretPartName")]
        public string? SecretPartNameOverride;

        /// <summary>
        ///     Container used to keep secret stash item.
        /// </summary>
        [ViewVariables]
        public ContainerSlot ItemContainer = default!;

        // todo remove
        public string SecretPartName => SecretPartNameOverride ?? Loc.GetString("comp-secret-stash-secret-part-name", ("name", Owner.Name));
    }
}
