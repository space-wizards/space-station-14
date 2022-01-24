using System.Collections.Generic;
using Content.Shared.Lathe;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMaterialStorageComponent))]
    public class MaterialStorageComponent : SharedMaterialStorageComponent
    {
        [ViewVariables]
        protected override Dictionary<string, int> Storage { get; set; } = new();

        /// <summary>
        ///     How much material the storage can store in total.
        /// </summary>
        [ViewVariables]
        public int StorageLimit => _storageLimit;
        [DataField("StorageLimit")]
        private int _storageLimit = -1;

        public override ComponentState GetComponentState()
        {
            return new MaterialStorageState(Storage);
        }

        /// <summary>
        ///     Checks if the storage can take a volume of material without surpassing its own limits.
        /// </summary>
        /// <param name="amount">The volume of material</param>
        /// <returns></returns>
        public bool CanTakeAmount(int amount)
        {
            return CurrentAmount + amount <= StorageLimit;
        }

        /// <summary>
        ///     Checks if it can insert a material.
        /// </summary>
        /// <param name="id">Material ID</param>
        /// <param name="amount">How much to insert</param>
        /// <returns>Whether it can insert the material or not.</returns>
        public bool CanInsertMaterial(string id, int amount)
        {
            return (CanTakeAmount(amount) || StorageLimit < 0) && (!Storage.ContainsKey(id) || Storage[id] + amount >= 0);
        }

        /// <summary>
        ///     Inserts material into the storage.
        /// </summary>
        /// <param name="id">Material ID</param>
        /// <param name="amount">How much to insert</param>
        /// <returns>Whether it inserted it or not.</returns>
        public bool InsertMaterial(string id, int amount)
        {
            if (!CanInsertMaterial(id, amount)) return false;

            if (!Storage.ContainsKey(id))
                Storage.Add(id, 0);

            Storage[id] += amount;

            Dirty();

            return true;
        }

        /// <summary>
        ///     Removes material from the storage.
        /// </summary>
        /// <param name="id">Material ID</param>
        /// <param name="amount">How much to remove</param>
        /// <returns>Whether it removed it or not.</returns>
        public bool RemoveMaterial(string id, int amount)
        {
            return InsertMaterial(id, -amount);
        }
    }
}
