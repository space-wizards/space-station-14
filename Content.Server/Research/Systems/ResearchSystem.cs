using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Research.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Research.Systems
{
    [UsedImplicitly]
    public sealed partial class ResearchSystem : SharedResearchSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLog = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly RadioSystem _radio = default!;

        public override void Initialize()
        {
            base.Initialize();
            InitializeClient();
            InitializeConsole();
            InitializeSource();
            InitializeServer();

            SubscribeLocalEvent<TechnologyDatabaseComponent, ResearchRegistrationChangedEvent>(OnDatabaseRegistrationChanged);
        }

        /// <summary>
        /// Gets a server based on it's unique numeric id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="serverUid"></param>
        /// <param name="serverComponent"></param>
        /// <returns></returns>
        public bool TryGetServerById(int id, [NotNullWhen(true)] out EntityUid? serverUid, [NotNullWhen(true)] out ResearchServerComponent? serverComponent)
        {
            serverUid = null;
            serverComponent = null;

            var query = EntityQueryEnumerator<ResearchServerComponent>();
            while (query.MoveNext(out var uid, out var server))
            {
                if (server.Id != id)
                    continue;
                serverUid = uid;
                serverComponent = server;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the names of all the servers.
        /// </summary>
        /// <returns></returns>
        public string[] GetServerNames()
        {
            var allServers = EntityQuery<ResearchServerComponent>(true).ToArray();
            var list = new string[allServers.Length];

            for (var i = 0; i < allServers.Length; i++)
            {
                list[i] = allServers[i].ServerName;
            }

            return list;
        }

        /// <summary>
        /// Gets the ids of all the servers
        /// </summary>
        /// <returns></returns>
        public int[] GetServerIds()
        {
            var allServers = EntityQuery<ResearchServerComponent>(true).ToArray();
            var list = new int[allServers.Length];

            for (var i = 0; i < allServers.Length; i++)
            {
                list[i] = allServers[i].Id;
            }

            return list;
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<ResearchServerComponent>();
            while (query.MoveNext(out var uid, out var server))
            {
                if (server.NextUpdateTime > _timing.CurTime)
                    continue;
                server.NextUpdateTime = _timing.CurTime + server.ResearchConsoleUpdateTime;

                UpdateServer(uid, (int) server.ResearchConsoleUpdateTime.TotalSeconds, server);
            }
        }
    }
}
