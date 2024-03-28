using Content.Shared.Storage.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components
{
    /// <summary>
    ///     Logic for a secret slot stash, like plant pot or toilet cistern.
    ///     Unlike <see cref="ItemSlotsComponent"/> it doesn't have interaction logic or verbs.
    ///     Other classes like <see cref="ToiletComponent"/> should implement it.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(SecretStashSystem))]
    public sealed partial class SecretStashComponent : Component
    {
        /// <summary>
        ///     Max item size that can be fitted into secret stash.
        /// </summary>
        [DataField("maxItemSize")]
        public ProtoId<ItemSizePrototype> MaxItemSize = "Small";

        /// <summary>
        /// If stash has way to open then this will switch between open and closed.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool ToggleOpen;

        /// <summary>
        /// Prying the door.
        /// </summary>
        [DataField]
        public float PryDoorTime = 1f;

        [DataField]
        public ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

        /// <summary>
        /// Is stash openable?.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool OpenableStash = false;

        /// <summary>
        ///     IC secret stash name. For example "the toilet cistern".
        ///     If empty string, will replace it with entity name in init.
        /// </summary>
        [DataField]
        public string SecretPartName { get; set; } = "";

        [DataField, AutoNetworkedField]
        public string ExamineStash = "comp-secret-stash-on-examine-found-hidden-item";

        /// <summary>
        ///     Container used to keep secret stash item.
        /// </summary>
        [ViewVariables]
        public ContainerSlot ItemContainer = default!;

    }

    /// <summary>
    /// Simple pry event for prying open a stash door.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed partial class StashPryDoAfterEvent : SimpleDoAfterEvent
    {
    }

    /// <summary>
    /// Visualizers for handling stash open closed state if stash has door.
    /// </summary>
    [Serializable, NetSerializable]
    public enum StashVisuals : byte
    {
        DoorVisualState,
    }

    [Serializable, NetSerializable]
    public enum DoorVisualState : byte
    {
        DoorOpen,
        DoorClosed
    }
}
