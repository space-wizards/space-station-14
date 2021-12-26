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
    ///     Logic for a secret slot stash, like plant pot or toilet cistern.
    ///     Unlike <see cref="ItemSlotsComponent"/> it doesn't have interaction logic or verbs.
    ///     Other classes like <see cref="ToiletComponent"/> should implement it.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(SecretStashSystem))]
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
        ///     If empty string, will replace it with entity name in init.
        /// </summary>
        [ViewVariables] [DataField("secretPartName")]
        public string SecretPartName = "";

        /// <summary>
        ///     Container used to keep secret stash item.
        /// </summary>
        [ViewVariables]
        public ContainerSlot ItemContainer = default!;

    }
}
