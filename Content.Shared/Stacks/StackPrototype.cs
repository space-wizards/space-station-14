#nullable enable
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Stacks
{
    [Prototype("stack")]
    public class StackPrototype : IPrototype
    {
        public string ID { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public SpriteSpecifier? Icon { get; private set; }

        /// <summary>
        ///     The entity id that will be spawned by default from this stack.
        /// </summary>
        public string? Spawn { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var reader = YamlObjectSerializer.NewReader(mapping);

            reader.DataField(this, x => x.ID, "id", string.Empty);
            reader.DataField(this, x => x.Name, "name", string.Empty);
            reader.DataField(this, x => x.Icon, "icon", null);
            reader.DataField(this, x => x.Spawn, "spawn", null);
        }
    }
}
