using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Maps;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Shared.Entry
{
    public sealed class EntryPoint : GameShared
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IResourceManager _resMan = default!;
#if DEBUG
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
#endif

        private readonly ResPath _ignoreFileDirectory = new("/IgnoredPrototypes/");

        public override void PreInit()
        {
            Dependencies.InjectDependencies(this);
        }

        public override void Shutdown()
        {
            _prototypeManager.PrototypesReloaded -= PrototypeReload;
        }

        public override void Init()
        {
            IgnorePrototypes();
        }

        public override void PostInit()
        {
            base.PostInit();

            InitTileDefinitions();
            Dependencies.Resolve<MarkingManager>().Initialize();

#if DEBUG
            _configurationManager.OverrideDefault(CVars.NetFakeLagMin, 0.075f);
            _configurationManager.OverrideDefault(CVars.NetFakeLoss, 0.005f);
            _configurationManager.OverrideDefault(CVars.NetFakeDuplicates, 0.005f);
#endif
        }

        private void InitTileDefinitions()
        {
            _prototypeManager.PrototypesReloaded += PrototypeReload;

            // Register space first because I'm a hard coding hack.
            var spaceDef = _prototypeManager.Index<ContentTileDefinition>(ContentTileDefinition.SpaceID);

            _tileDefinitionManager.Register(spaceDef);

            var prototypeList = new List<ContentTileDefinition>();
            foreach (var tileDef in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
            {
                if (tileDef.ID == ContentTileDefinition.SpaceID)
                {
                    continue;
                }

                prototypeList.Add(tileDef);
            }

            // Sort ordinal to ensure it's consistent client and server.
            // So that tile IDs match up.
            prototypeList.Sort((a, b) => string.Compare(a.ID, b.ID, StringComparison.Ordinal));

            foreach (var tileDef in prototypeList)
            {
                _tileDefinitionManager.Register(tileDef);
            }

            _tileDefinitionManager.Initialize();
        }

        private void PrototypeReload(PrototypesReloadedEventArgs obj)
        {
            /* I am leaving this here commented out to re-iterate
             - our game is shitcode
             - tiledefmanager no likey proto reloads and you must re-assign the tile ids.
            if (!obj.WasModified<ContentTileDefinition>())
                return;
                */

            // Need to re-allocate tiledefs due to how prototype reloads work
            foreach (var def in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
            {
                def.AssignTileId(_tileDefinitionManager[def.ID].TileId);
            }
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
