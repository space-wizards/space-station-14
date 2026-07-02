using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Salvage;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Content.Server.Chat.Managers;
using Content.Server.Gravity;
using Content.Server.Parallax;
using Content.Server.Procedural;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Construction.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server.Salvage
{
    public sealed partial class SalvageSystem : SharedSalvageSystem
    {
        [Dependency] private IChatManager _chat = default!;
        [Dependency] private IConfigurationManager _configurationManager = default!;
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private AnchorableSystem _anchorable = default!;
        [Dependency] private BiomeSystem _biome = default!;
        [Dependency] private DungeonSystem _dungeon = default!;
        [Dependency] private GravitySystem _gravity = default!;
        [Dependency] private LabelSystem _labelSystem = default!;
        [Dependency] private MapLoaderSystem _loader = default!;
        [Dependency] private MetaDataSystem _metaData = default!;
        [Dependency] private RadioSystem _radioSystem = default!;
        [Dependency] private SharedAudioSystem _audio = default!;
        [Dependency] private SharedTransformSystem _transform = default!;
        [Dependency] private SharedMapSystem _mapSystem = default!;
        [Dependency] private ShuttleSystem _shuttle = default!;
        [Dependency] private ShuttleConsoleSystem _shuttleConsoles = default!;
        [Dependency] private StationSystem _station = default!;
        [Dependency] private UserInterfaceSystem _ui = default!;
        [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeExpeditions();
            InitializeMagnet();
            InitializeRunner();
        }

        private void Report(EntityUid source, string channelName, string messageKey, params (string, object)[] args)
        {
            var message = args.Length == 0 ? Loc.GetString(messageKey) : Loc.GetString(messageKey, args);
            var channel = ProtoMan.Index<RadioChannelPrototype>(channelName);
            _radioSystem.SendRadioMessage(source, message, channel, source);
        }

        public override void Update(float frameTime)
        {
            UpdateExpeditions();
            UpdateMagnet();
            UpdateRunner();
        }
    }
}

