using System;
using System.Collections.Generic;
using Content.Shared.Materials;
using Content.Shared.Research;
using Mono.Cecil;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Research
{
    public enum LatheType
    {
        Autolathe,
        Protolathe,
    }

    public class SharedLatheComponent : Component
    {
        public override string Name => "Lathe";
        public override uint? NetID => ContentNetIDs.LATHE;
        public LatheType LatheType = LatheType.Autolathe;

        public bool CanProduce(LatheRecipePrototype recipe, int quantity = 1)
        {
            Owner.TryGetComponent(out SharedMaterialStorageComponent storage);

            if (storage == null) return false;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                if (storage[material] <= (amount * quantity)) return false;
            }

            return true;
        }

        public bool CanProduce(string ID, int quantity = 1)
        {
            Owner.TryGetComponent(out SharedMaterialStorageComponent storage);

            if (storage == null) return false;

            var protMan = IoCManager.Resolve<IPrototypeManager>();
            protMan.TryIndex(ID, out LatheRecipePrototype recipe);

            if (recipe == null) return false;

            foreach (var (material, amount) in recipe.RequiredMaterials)
            {
                if (storage[material] <= (amount * quantity)) return false;
            }

            return true;
        }

        [Serializable, NetSerializable]
        public class LatheMenuOpenMessage : ComponentMessage
        {
            public LatheMenuOpenMessage()
            {
                Directed = true;
            }
        }

        [Serializable, NetSerializable]
        public class LatheProducingRecipeMessage : ComponentMessage
        {
            public readonly string ID;
            public LatheProducingRecipeMessage(string id)
            {
                Directed = true;
                ID = id;
            }
        }

        [Serializable, NetSerializable]
        public class LatheStoppedProducingRecipeMessage : ComponentMessage
        {
            public LatheStoppedProducingRecipeMessage()
            {
                Directed = true;
            }
        }

        [Serializable, NetSerializable]
        public class LatheFullQueueMessage : ComponentMessage
        {
            public readonly Queue<string> Recipes;
            public LatheFullQueueMessage(Queue<string> recipes)
            {
                Directed = true;
                Recipes = recipes;
            }
        }

        [Serializable, NetSerializable]
        public class LatheQueueRecipeMessage : ComponentMessage
        {
            public readonly string ID;
            public readonly int Quantity;
            public LatheQueueRecipeMessage(string id, int quantity)
            {
                Directed = true;
                ID = id;
                Quantity = quantity;
            }
        }
    }
}
