using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Research
{
    public class MaterialStorageComponent : SharedMaterialStorageComponent
    {
        private Dictionary<string, int> _storage = new Dictionary<string, int>();
        protected override Dictionary<string, int> Storage => _storage;

        /// <summary>
        ///     How much material the storage can store in total.
        /// </summary>
        public int StorageLimit => _storageLimit;
        private int _storageLimit;

        public bool InsertMaterial(string ID, int amount)
        {
            if ((CurrentAmount + amount > StorageLimit && StorageLimit >= 0) || (Storage.ContainsKey(ID) && Storage[ID] + amount < 0)) return false;

            if (!Storage.ContainsKey(ID))
                _storage.Add(ID, 0);

            _storage[ID] += amount;

            SendNetworkMessage(new MaterialStorageUpdateMessage(Storage));

            return true;
        }

        public bool RemoveMaterial(string ID, int amount)
        {
            return InsertMaterial(ID, -amount);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _storageLimit, "StorageLimit", -1);
        }
    }
}
