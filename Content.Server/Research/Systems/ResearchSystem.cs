using Content.Server.Research.Components;
using Content.Shared.Research.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Research
{
    [UsedImplicitly]
    public sealed partial class ResearchSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        private const int ResearchConsoleUIUpdateTime = 5;

        private float _timer = ResearchConsoleUIUpdateTime;
        private readonly List<ResearchServerComponent> _servers = new();
        public IReadOnlyList<ResearchServerComponent> Servers => _servers;

        public override void Initialize()
        {
            base.Initialize();
            InitializeClient();
            InitializeConsole();
            InitializeServer();
            InitializeTechnology();
        }

        public bool RegisterServer(ResearchServerComponent server)
        {
            if (_servers.Contains(server)) return false;
            _servers.Add(server);
            _servers[^1].Id = _servers.Count - 1;
            return true;
        }

        public void UnregisterServer(ResearchServerComponent server)
        {
            _servers.Remove(server);
        }

        public ResearchServerComponent? GetServerById(int id)
        {
            foreach (var server in Servers)
            {
                if (server.Id == id) return server;
            }

            return null;
        }

        public string[] GetServerNames()
        {
            var list = new string[Servers.Count];

            for (var i = 0; i < Servers.Count; i++)
            {
                list[i] = Servers[i].ServerName;
            }

            return list;
        }

        public int[] GetServerIds()
        {
            var list = new int[Servers.Count];

            for (var i = 0; i < Servers.Count; i++)
            {
                list[i] = Servers[i].Id;
            }

            return list;
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            while (_timer > ResearchConsoleUIUpdateTime)
            {
                foreach (var server in _servers)
                {
                    UpdateServer(server, ResearchConsoleUIUpdateTime);
                }

                foreach (var console in EntityManager.EntityQuery<ResearchConsoleComponent>())
                {
                    if (!_uiSystem.IsUiOpen(console.Owner, ResearchConsoleUiKey.Key)) continue;
                    UpdateConsoleInterface(console);
                }

                _timer -= ResearchConsoleUIUpdateTime;
            }
        }
    }
}
