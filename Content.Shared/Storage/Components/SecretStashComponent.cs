using Content.Shared.Storage.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Content.Shared.Whitelist;

namespace Content.Shared.Storage.Components
{
    /// <summary>
    ///     Logic for a secret slot stash, like plant pot or toilet cistern.
    ///     Unlike <see cref="ItemSlotsComponent"/> it has logic for opening and closing
    ///     the stash.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(SecretStashSystem))]
    public sealed partial class SecretStashComponent : Component
    {
        /// <summary>
        ///     Max item size that can be inserted into secret stash.
        /// </summary>
        [DataField("maxItemSize")]
        public ProtoId<ItemSizePrototype> MaxItemSize = "Small";

        /// <summary>
        ///     Entity blacklist for secret stashes.
        /// </summary>
        [DataField]
        public EntityWhitelist? Blacklist;

        /// <summary>
        ///     This sound will be played when you try to insert an item in the stash.
        ///     The sound will be played whether or not the item is actually inserted.
        /// </summary>
        [DataField]
        public SoundSpecifier? TryInsertItemSound;

        /// <summary>
        ///     This sound will be played when you try to remove an item in the stash.
        ///     The sound will be played whether or not the item is actually removed.
        /// </summary>
        [DataField]
        public SoundSpecifier? TryRemoveItemSound;

        /// <summary>
        ///     If true, verbs will appear to help interact with the stash.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool HasVerbs = true;

        /// <summary>
        ///     The name of the secret stash. For example "the toilet cistern".
        ///     If null, the name of the entity will be used instead.
        /// </summary>
        [DataField]
        public string? SecretStashName;

        /// <summary>
        ///     Container used to keep secret stash item.
        /// </summary>
        [ViewVariables]
        public ContainerSlot ItemContainer = default!;
    }
}
