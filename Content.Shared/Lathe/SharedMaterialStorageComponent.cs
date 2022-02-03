using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Lathe
{
    [NetworkedComponent()]
    public class SharedMaterialStorageComponent : Component, IEnumerable<KeyValuePair<string, int>>
    {
        [ViewVariables]
        protected virtual Dictionary<string, int> Storage { get; set; } = new();

        public int this[string id]
        {
            get
            {
                if (!Storage.ContainsKey(id))
                    return 0;
                return Storage[id];
            }
        }

        public int this[MaterialPrototype material]
        {
            get
            {
                var id = material.ID;
                if (!Storage.ContainsKey(id))
                    return 0;
                return Storage[id];
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
    }

    [NetSerializable, Serializable]
    public class MaterialStorageState : ComponentState
    {
        public readonly Dictionary<string, int> Storage;
        public MaterialStorageState(Dictionary<string, int> storage)
        {
            Storage = storage;
        }
    }
}
