using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Server.AI.Utility
{
    [Prototype("behaviorSet")]
    public class BehaviorSetPrototype : IPrototype
    {
        /// <summary>
        ///     Name of the BehaviorSet.
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        ///     Actions that this BehaviorSet grants to the entity.
        /// </summary>
        public IReadOnlyList<string> Actions { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(this, x => x.ID, "id", string.Empty);
            serializer.DataField(this, x => x.Actions, "actions", new List<string>());
        }
    }
}
