using Robust.Shared.Serialization;

namespace Content.Shared.Lathe;

        /// <summary>
        ///     Sent to the server to sync material storage and the recipe queue.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class LatheSyncRequestMessage : BoundUserInterfaceMessage
        {
            public LatheSyncRequestMessage()
            {
            }
        }

        /// <summary>
        ///     Sent to the server to sync the lathe's technology database with the research server.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class LatheServerSyncMessage : BoundUserInterfaceMessage
        {
            public LatheServerSyncMessage()
            {
            }
        }

        /// <summary>
        ///     Sent to the server to open the ResearchClient UI.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class LatheServerSelectionMessage : BoundUserInterfaceMessage
        {
            public LatheServerSelectionMessage()
            {
            }
        }

        /// <summary>
        ///     Sent to the client when the lathe is producing a recipe.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class LatheProducingRecipeMessage : BoundUserInterfaceMessage
        {
            public readonly string ID;
            public LatheProducingRecipeMessage(string id)
            {
                ID = id;
            }
        }

        /// <summary>
        ///     Sent to the client when the lathe stopped/finished producing a recipe.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class LatheStoppedProducingRecipeMessage : BoundUserInterfaceMessage
        {
            public LatheStoppedProducingRecipeMessage()
            {
            }
        }

        /// <summary>
        ///     Sent to the client to let it know about the recipe queue.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class LatheFullQueueMessage : BoundUserInterfaceMessage
        {
            public readonly List<string> Recipes;
            public LatheFullQueueMessage(List<string> recipes)
            {
                Recipes = recipes;
            }
        }

        /// <summary>
        ///     Sent to the server when a client queues a new recipe.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class LatheQueueRecipeMessage : BoundUserInterfaceMessage
        {
            public readonly string ID;
            public readonly int Quantity;
            public LatheQueueRecipeMessage(string id, int quantity)
            {
                ID = id;
                Quantity = quantity;
            }
        }

        [NetSerializable, Serializable]
        public enum LatheUiKey
        {
            Key,
        }
