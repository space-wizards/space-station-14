using System.Collections.Generic;
using Content.Server.GameObjects.Components.Research;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    public class ResearchSystem : EntitySystem
    {
        public const float ResearchConsoleUIUpdateTime = 30f;

        private float _timer = ResearchConsoleUIUpdateTime;
        private readonly List<ResearchServerComponent> _servers = new();
        private readonly IEntityQuery ConsoleQuery;
        public IReadOnlyList<ResearchServerComponent> Servers => _servers;

        public ResearchSystem()
        {
            ConsoleQuery = new TypeEntityQuery(typeof(ResearchConsoleComponent));
        }

        public bool RegisterServer(ResearchServerComponent server)
        {
            if (_servers.Contains(server)) return false;
            _servers.Add(server);
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

            foreach (var server in _servers)
            {
                server.Update(frameTime);
            }

            if (_timer >= ResearchConsoleUIUpdateTime)
            {
                foreach (var console in EntityManager.GetEntities(ConsoleQuery))
                {
                    console.GetComponent<ResearchConsoleComponent>().UpdateUserInterface();
                }

                _timer = 0f;
            }
        }
    }
}
