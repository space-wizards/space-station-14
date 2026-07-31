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
using Content.Server.Power.EntitySystems; //DS14

namespace Content.Server.Research.Systems
{
    [UsedImplicitly]
    public sealed partial class ResearchSystem : SharedResearchSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLog = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly RadioSystem _radio = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!; //DS14

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
        /// Gets a server based on its unique numeric id.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        /// <param name="serverUid"></param>
        /// <param name="serverComponent"></param>
        /// <returns></returns>
        // DS14-edit-start
        public HashSet<Entity<ResearchServerComponent>> GetServers(EntityUid client)
        {
            var servers = new HashSet<Entity<ResearchServerComponent>>();

            var clientXform = Transform(client);
            if (clientXform.GridUid is not { } grid)
                return servers;

            _lookup.GetGridEntities(grid, servers);
            servers.RemoveWhere(server => !Transform(server).Anchored || !_power.IsPowered(server.Owner));
            return servers;
        }
        // DS14-edit-end

        /// <summary>
        /// Gets the names of all the servers.
        /// </summary>
        /// <returns></returns>
        public string[] GetServerNames(EntityUid client)
        {
            return GetServers(client).Select(x => x.Comp.ServerName).ToArray();
        }

        /// <summary>
        /// Gets the ids of all the servers
        /// </summary>
        /// <returns></returns>
        public int[] GetServerIds(EntityUid client)
        {
            return GetServers(client).Select(x => x.Comp.Id).ToArray();
        }

        // DS14-start
        public string[] GetServerNames(EntityUid client, bool isTaipan)
        {
            var allServers = GetServers(client).ToArray();
            var list = new List<string>();

            foreach (var server in allServers)
            {
                if (server.Comp.isTaipan == isTaipan)
                    list.Add(server.Comp.ServerName);
            }

            return list.ToArray();
        }

        public int[] GetServerIds(EntityUid client, bool isTaipan)
        {
            var allServers = GetServers(client).ToArray();
            var list = new List<int>();

            foreach (var server in allServers)
            {
                if (server.Comp.isTaipan == isTaipan)
                    list.Add(server.Comp.Id);
            }

            return list.ToArray();
        }

        public bool TryGetServerById(EntityUid client, int id, [NotNullWhen(true)] out EntityUid? serverUid, [NotNullWhen(true)] out ResearchServerComponent? serverComponent)
        {
            serverUid = null;
            serverComponent = null;

            var query = GetServers(client).ToList();
            foreach (var (uid, server) in query)
            {
                if (server.Id != id)
                    continue;
                serverUid = uid;
                serverComponent = server;
                return true;
            }

            return false;
        // DS14-end
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
