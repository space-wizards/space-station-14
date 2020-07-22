using System.Collections.Generic;
using Content.Server.Interfaces;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Atmos
{
    [Prototype("gasReaction")]
    public class GasReactionPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; }

        /// <summary>
        ///     Lower numbers are checked/react later than higher numbers.
        ///     If two reactions have the same priority, they may happen in either order.
        /// </summary>
        public int Priority { get; }
        private List<IGasReactionEffect> _effects;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => ID, "id", string.Empty);
            serializer.DataField(this, x => Priority, "priority", 100);
        }
    }
}
