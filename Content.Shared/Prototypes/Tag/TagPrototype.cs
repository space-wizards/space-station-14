#nullable enable
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.Tag
{
    /// <summary>
    ///     Prototype representing a tag in YAML.
    ///     Meant to only have an ID property, as that is the only thing that
    ///     gets saved in TagComponent.
    /// </summary>
    [Prototype("Tag")]
    public class TagPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; [UsedImplicitly] private set; } = default!;

        private void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.ID, "id", "");
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);
            ExposeData(serializer);
        }
    }
}
