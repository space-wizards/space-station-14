using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Content.Shared.Humanoid.Markings;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Shared.Entry
{
    public sealed partial class EntryPoint : GameShared
    {
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        [Dependency] private IResourceManager _resMan = default!;
#if DEBUG
        [Dependency] private IConfigurationManager _configurationManager = default!;
#endif

        private readonly ResPath _ignoreFileDirectory = new("/IgnoredPrototypes/");

        public override void PreInit()
        {
            Dependencies.InjectDependencies(this);
        }

        public override void Init()
        {
            IgnorePrototypes();
        }

        public override void PostInit()
        {
            base.PostInit();

            Dependencies.Resolve<MarkingManager>().Initialize();

#if DEBUG
            _configurationManager.OverrideDefault(CVars.NetFakeLagMin, 0.075f);
            _configurationManager.OverrideDefault(CVars.NetFakeLoss, 0.005f);
            _configurationManager.OverrideDefault(CVars.NetFakeDuplicates, 0.005f);
#endif
        }

        private void IgnorePrototypes()
        {
            if (!TryReadFile(out var sequences))
                return;

            foreach (var sequence in sequences)
            {
                foreach (var node in sequence.Sequence)
                {
                    var path = new ResPath(((ValueDataNode) node).Value);

                    if (string.IsNullOrEmpty(path.Extension))
                    {
                        _prototypeManager.AbstractDirectory(path);
                    }
                    else
                    {
                        _prototypeManager.AbstractFile(path);
                    }
                }
            }
        }

        private bool TryReadFile([NotNullWhen(true)] out List<SequenceDataNode>? sequence)
        {
            sequence = new();

            foreach (var path in _resMan.ContentFindFiles(_ignoreFileDirectory))
            {
                if (!_resMan.TryContentFileRead(path, out var stream))
                    continue;

                using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
                var documents = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();

                if (documents == null)
                    continue;

                sequence.Add((SequenceDataNode) documents.Root);
            }
            return true;
        }
    }
}
