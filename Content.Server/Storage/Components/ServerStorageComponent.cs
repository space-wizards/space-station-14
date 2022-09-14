using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using System.Threading;

namespace Content.Server.Storage.Components
{
    /// <summary>
    /// Storage component for containing entities within this one, matches a UI on the client which shows stored entities
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedStorageComponent))]
    public sealed class ServerStorageComponent : SharedStorageComponent
    {
        public string LoggerName = "Storage";

        public Container? Storage;

        public readonly Dictionary<EntityUid, int> SizeCache = new();

        private bool _occludesLight = true;

        [DataField("quickInsert")]
        public bool QuickInsert = false; // Can insert storables by "attacking" them with the storage entity

        [DataField("clickInsert")]
        public bool ClickInsert = true; // Can insert stuff by clicking the storage entity with it

        [DataField("areaInsert")]
        public bool AreaInsert = false;  // "Attacking" with the storage entity causes it to insert all nearby storables after a delay

        /// <summary>
        /// Token for interrupting area insert do after.
        /// </summary>
        public CancellationTokenSource? CancelToken;

        [DataField("areaInsertRadius")]
        public int AreaInsertRadius = 1;

        [DataField("whitelist")]
        public EntityWhitelist? Whitelist = null;
        [DataField("blacklist")]
        public EntityWhitelist? Blacklist = null;

        /// <summary>
        ///     If true, storage will show popup messages to the player after failed interactions.
        ///     Usually this is message that item doesn't fit inside container.
        /// </summary>
        [DataField("popup")]
        public bool ShowPopup = true;

        /// <summary>
        /// This storage has an open UI
        /// </summary>
        public bool IsOpen = false;
        public int StorageUsed;
        [DataField("capacity")]
        public int StorageCapacityMax = 10000;

        [DataField("storageOpenSound")]
        public SoundSpecifier? StorageOpenSound { get; set; } = new SoundCollectionSpecifier("storageRustle");

        [DataField("storageInsertSound")]
        public SoundSpecifier? StorageInsertSound { get; set; } = new SoundCollectionSpecifier("storageRustle");

        [DataField("storageRemoveSound")]
        public SoundSpecifier? StorageRemoveSound { get; set; }
        [DataField("storageCloseSound")]
        public SoundSpecifier? StorageCloseSound { get; set; }

        [ViewVariables]
        public override IReadOnlyList<EntityUid>? StoredEntities => Storage?.ContainedEntities;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("occludesLight")]
        public bool OccludesLight
        {
            get => _occludesLight;
            set
            {
                _occludesLight = value;
                if (Storage != null) Storage.OccludesLight = value;
            }
        }

        // neccesary for abstraction, should be deleted on complete storage ECS
        public override bool Remove(EntityUid entity)
        {
            return true;
        }
    }
}
