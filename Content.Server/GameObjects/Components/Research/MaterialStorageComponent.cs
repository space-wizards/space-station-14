using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedMaterialStorageComponent))]
    public class MaterialStorageComponent : SharedMaterialStorageComponent
    {
        [ViewVariables]
        protected override Dictionary<string, int> Storage { get; set; } = new Dictionary<string, int>();

        /// <summary>
        ///     How much material the storage can store in total.
        /// </summary>
        [ViewVariables]
        public int StorageLimit => _storageLimit;
        private int _storageLimit;

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
        /// <param name="ID">Material ID</param>
        /// <param name="amount">How much to insert</param>
        /// <returns>Whether it can insert the material or not.</returns>
        public bool CanInsertMaterial(string ID, int amount)
        {
            return (CanTakeAmount(amount) || StorageLimit < 0) && (!Storage.ContainsKey(ID) || Storage[ID] + amount >= 0);
        }

        /// <summary>
        ///     Inserts material into the storage.
        /// </summary>
        /// <param name="ID">Material ID</param>
        /// <param name="amount">How much to insert</param>
        /// <returns>Whether it inserted it or not.</returns>
        public bool InsertMaterial(string ID, int amount)
        {
            if (!CanInsertMaterial(ID, amount)) return false;

            if (!Storage.ContainsKey(ID))
                Storage.Add(ID, 0);

            Storage[ID] += amount;

            Dirty();

            return true;
        }

        /// <summary>
        ///     Removes material from the storage.
        /// </summary>
        /// <param name="ID">Material ID</param>
        /// <param name="amount">How much to remove</param>
        /// <returns>Whether it removed it or not.</returns>
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
