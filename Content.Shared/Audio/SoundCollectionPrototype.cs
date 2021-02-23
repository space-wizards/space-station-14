#nullable enable
using System.Collections.Generic;
using System.Data;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Audio
{
    [Prototype("soundCollection")]
    public sealed class SoundCollectionPrototype : IPrototype
    {
        public string ID { get; private set; } = string.Empty;
        public List<string> PickFiles { get; private set; } = new();

        public void LoadFrom(YamlMappingNode mapping)
        {
            ID = mapping.GetNode("id").AsString();

            // In the unlikely case the method gets called twice
            PickFiles.Clear();

            foreach (var file in mapping.GetNode<YamlSequenceNode>("files"))
            {
                PickFiles.Add(file.AsString());
            }
        }
    }
}
