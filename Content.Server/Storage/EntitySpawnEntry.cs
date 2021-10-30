using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Storage
{
    /// <summary>
    /// Dictates a list of items that can be spawned.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public struct EntitySpawnEntry : IPopulateDefaultValues
    {
        [DataField("id", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrototypeId;

        /// <summary>
        /// The probability that an item will spawn. Takes decimal form so 0.05 is 5%, 0.50 is 50% etc.
        /// </summary>
        [DataField("prob")]
        public float SpawnProbability;

        /// <summary>
        /// orGroup signifies to pick between entities designated with an ID.
        ///
        /// <example>
        /// <para>To define an orGroup in a StorageFill component you
        /// need to add it to the entities you want to choose between and
        /// add a prob field. In this example there is a 50% chance the storage
        /// spawns with Y or Z.
        ///
        /// </para>
        /// <code>
        /// - type: StorageFill
        ///   contents:
        ///     - name: X
        ///     - name: Y
        ///       prob: 0.50
        ///       orGroup: YOrZ
        ///     - name: Z
        ///       orGroup: YOrZ
        /// </code>
        /// </example>
        /// </summary>
        [DataField("orGroup")]
        public string? GroupId;

        [DataField("amount")]
        public int Amount;

        public void PopulateDefaultValues()
        {
            Amount = 1;
            SpawnProbability = 1;
        }
    }
}
