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

        public override void Init()
        {
        }

        public override void PostInit()
        {
            base.PostInit();

            InitTileDefinitions();
            IoCManager.Resolve<MarkingManager>().Initialize();

            var configMan = IoCManager.Resolve<IConfigurationManager>();
#if DEBUG
            configMan.OverrideDefault(CVars.NetFakeLagMin, 0.075f);
            configMan.OverrideDefault(CVars.NetFakeLoss, 0.005f);
            configMan.OverrideDefault(CVars.NetFakeDuplicates, 0.005f);

            // fake lag rand leads to messages arriving out of order. Sadly, networking is not robust enough, so for now
            // just leaving this disabled.
            // configMan.OverrideDefault(CVars.NetFakeLagRand, 0.01f);
#endif
        }

        private void InitTileDefinitions()
        {
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
    }
}
