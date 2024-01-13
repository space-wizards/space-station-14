using Content.Shared.CCVar;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IoC;
using Content.Shared.Localizations;
using Content.Shared.Maps;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Entry
{
    public sealed class EntryPoint : GameShared
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        public override void PreInit()
        {
            IoCManager.InjectDependencies(this);
            SharedContentIoC.Register();
        }

        public override void Shutdown()
        {
            _prototypeManager.PrototypesReloaded -= PrototypeReload;
        }

        public override void Init()
        {
        }

        public override void PostInit()
        {
            base.PostInit();

            InitTileDefinitions();
            IoCManager.Resolve<MarkingManager>().Initialize();

#if DEBUG
            var configMan = IoCManager.Resolve<IConfigurationManager>();
            configMan.OverrideDefault(CVars.NetFakeLagMin, 0.075f);
            configMan.OverrideDefault(CVars.NetFakeLoss, 0.005f);
            configMan.OverrideDefault(CVars.NetFakeDuplicates, 0.005f);
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
    }
}
