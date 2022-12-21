using System.Linq;
using Content.Server.Research.Components;
using Content.Shared.Research.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Research.Systems
{
    [UsedImplicitly]
    public sealed partial class ResearchSystem : SharedResearchSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchServerComponent, ComponentStartup>(OnStartup);

            InitializeClient();
            InitializeConsole();
            InitializeSource();
        }

        private void OnStartup(EntityUid uid, ResearchServerComponent component, ComponentStartup args)
        {
            var unusedId = EntityQuery<ResearchServerComponent>(true)
                .Max(s => s.Id) + 1;
            component.Id = unusedId;
        }

        public ResearchServerComponent? GetServerById(int id)
        {
            foreach (var server in EntityQuery<ResearchServerComponent>())
            {
                if (server.Id == id)
                    return server;
            }

            return null;
        }

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
            foreach (var server in EntityQuery<ResearchServerComponent>())
            {
                if (server.NextUpdateTime > _timing.CurTime)
                    continue;
                server.NextUpdateTime = _timing.CurTime + server.ResearchConsoleUpdateTime;

                UpdateServer(server, (int) server.ResearchConsoleUpdateTime.TotalSeconds);
            }
        }
    }
}
