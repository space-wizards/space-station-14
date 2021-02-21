#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Server.GameObjects.Components.AI
{
    [Prototype("aiFaction")]
    public class AiFactionPrototype : IPrototype
    {
        // These are immutable so any dynamic changes aren't saved back over.
        // AiFactionSystem will just read these and then store them.

        public string ID { get; private set; } = default!;

        public IReadOnlyList<string> Hostile { get; private set; } = default!;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => x.ID, "id", string.Empty);
            serializer.DataField(this, x => x.Hostile, "hostile", new List<string>());
        }
    }
}
