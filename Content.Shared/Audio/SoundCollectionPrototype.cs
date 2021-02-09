using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Audio
{
    [Prototype("soundCollection")]
    public sealed class SoundCollectionPrototype : IPrototype, IIndexedPrototype
    {
        [YamlField("id")]
        public string ID { get; private set; }

        [YamlField("files")] private List<string> _pickFiles;

        public List<string> PickFiles
        {
            get => _pickFiles;
            private set => _pickFiles = value;
        }
    }
}
