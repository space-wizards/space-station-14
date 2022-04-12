using Content.Shared.Sound;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Server.Player;
using Robust.Shared.Containers;

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

        [DataField("occludesLight")]
        public bool OccludesLight = true;

        [DataField("quickInsert")]
        public bool QuickInsert = false; // Can insert storables by "attacking" them with the storage entity

        [DataField("clickInsert")]
        public bool ClickInsert = true; // Can insert stuff by clicking the storage entity with it

        [DataField("areaInsert")]
        public bool AreaInsert = false;  // "Attacking" with the storage entity causes it to insert all nearby storables after a delay
        [DataField("areaInsertRadius")]
        public int AreaInsertRadius = 1;

        [DataField("whitelist")]
        public EntityWhitelist? Whitelist = null;

        public bool StorageInitialCalculated;
        public int StorageUsed;
        [DataField("capacity")]
        public int StorageCapacityMax = 10000;

        [DataField("storageSoundCollection")]
        public SoundSpecifier StorageSoundCollection { get; set; } = new SoundCollectionSpecifier("storageRustle");

        [ViewVariables]
        public override IReadOnlyList<EntityUid>? StoredEntities => Storage?.ContainedEntities;

        // [ViewVariables(VVAccess.ReadWrite)]
        // public bool OccludesLight
        // {
        //     get => _occludesLight;
        //     set
        //     {
        //         _occludesLight = value;
        //         if (Storage != null) Storage.OccludesLight = value;
        //     }
        // }
        public override bool Remove(EntityUid entity)
        {
            return true;
        }
    }
}
