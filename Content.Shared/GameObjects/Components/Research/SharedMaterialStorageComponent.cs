using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Research
{
    public class SharedMaterialStorageComponent : Component, IEnumerable<KeyValuePair<string, int>>
    {
        [ViewVariables]
        protected virtual Dictionary<string, int> Storage { get; set; }
        public override string Name => "MaterialStorage";
        public override uint? NetID => ContentNetIDs.MATERIAL_STORAGE;

        public int this[string ID]
        {
            get
            {
                if (!Storage.ContainsKey(ID))
                    return 0;
                return Storage[ID];
            }
        }

        public int this[Material material]
        {
            get
            {
                var ID = material.ID;
                if (!Storage.ContainsKey(ID))
                    return 0;
                return Storage[ID];
            }
        }

        /// <summary>
        ///     The total volume of material stored currently.
        /// </summary>
        [ViewVariables] public int CurrentAmount
        {
            get
            {
                var value = 0;

                foreach (var amount in Storage.Values)
                {
                    value += amount;
                }

                return value;
            }
        }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return Storage.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [NetSerializable, Serializable]
        public class MaterialStorageUpdateMessage : ComponentMessage
        {
            public readonly Dictionary<string, int> Storage;

            public MaterialStorageUpdateMessage(Dictionary<string, int> storage)
            {
                Directed = true;
                Storage = storage;
            }
        }
    }
}
