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
        private readonly ResPath _partialFileDirectory = new("/PartialPrototypes/");

        public override void PreInit()
        {
            Dependencies.InjectDependencies(this);

            // This is where you should mark directories as partial downstream
            // For documentation on how partial prototypes work,
            // see the documentation on IPrototypeManager.PartialDirectory
            // You can uncomment the code below and navigate to the method to see it.
            // The order matters, partials are applied in the order in which they are marked here.
            // For example, if two directories modify the same prototype, the first patch will be applied first,
            // and then the second.
            // _prototypeManager.PartialDirectory(new ResPath("/Prototypes/_ForkOne"));
            // _prototypeManager.PartialDirectory(new ResPath("/Prototypes/_ForkTwo"));
        }

        public override void Init()
        {
            IgnorePrototypes();
            PartialPrototypes();
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
            foreach (var path in TryReadFilesPaths(_ignoreFileDirectory))
            {
                if (string.IsNullOrEmpty(path.Extension))
                    _prototypeManager.AbstractDirectory(path);
                else
                    _prototypeManager.AbstractFile(path);
            }
        }

        private void PartialPrototypes()
        {
            var i = 0;
            foreach (var path in TryReadFilesPaths(_partialFileDirectory))
            {
                if (string.IsNullOrEmpty(path.Extension))
                    _prototypeManager.PartialDirectory(path, i++);
                else
                    _prototypeManager.PartialFile(path, i++);
            }
        }

        private IEnumerable<ResPath> TryReadFilesPaths(ResPath directory)
        {
            foreach (var path in _resMan.ContentFindFiles(directory).OrderBy(p => p.CanonPath))
            {
                if (!_resMan.TryContentFileRead(path, out var stream))
                    continue;

                using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
                var documents = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();

                if (documents == null)
                    continue;

                var sequence = (SequenceDataNode) documents.Root;
                foreach (var node in sequence.Sequence)
                {
                    yield return new ResPath(((ValueDataNode) node).Value);
                }
            }
        }
    }
}
