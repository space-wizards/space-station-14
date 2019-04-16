using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Audio
{
    [Prototype("sound_collection")]
    public sealed class SoundCollectionPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; }
        public IReadOnlyList<string> PickFiles { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();

            var pickFiles = new List<string>();

            foreach (var file in mapping.GetNode<YamlSequenceNode>("files"))
            {
                pickFiles.Add(file.AsString());
            }

            PickFiles = pickFiles;
        }
    }
}
