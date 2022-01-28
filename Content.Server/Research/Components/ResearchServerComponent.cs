using System.Collections.Generic;
using Content.Server.Power.Components;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Research.Components
{
    [RegisterComponent]
    public class ResearchServerComponent : Component
    {
        public static int ServerCount = 0;

        public override string Name => "ResearchServer";

        [ViewVariables(VVAccess.ReadWrite)] public string ServerName => _serverName;

        [DataField("servername")]
        private string _serverName = "RDSERVER";
        private float _timer = 0f;
        public TechnologyDatabaseComponent? Database { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)] [DataField("points")] private int _points = 0;

        [ViewVariables(VVAccess.ReadOnly)] public int Id { get; private set; }

        // You could optimize research by keeping a list of unlocked recipes too.
        [ViewVariables(VVAccess.ReadOnly)]
        public IReadOnlyList<TechnologyPrototype>? UnlockedTechnologies => Database?.Technologies;

        [ViewVariables(VVAccess.ReadOnly)]
        public List<ResearchPointSourceComponent> PointSources { get; } = new();

        [ViewVariables(VVAccess.ReadOnly)]
        public List<ResearchClientComponent> Clients { get; } = new();

        public int Point => _points;

        /// <summary>
        ///     How many points per second this R&D server gets.
        ///     The value is calculated from all point sources connected to it.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public int PointsPerSecond
        {
            // This could be changed to PointsPerMinute quite easily for optimization.
            get
            {
                var points = 0;

                if (CanRun)
                {
                    foreach (var source in PointSources)
                    {
                        if (source.CanProduce) points += source.PointsPerSecond;
                    }
                }

                return points;
            }
        }

        /// <remarks>If no <see cref="ApcPowerReceiverComponent"/> is found, it's assumed power is not required.</remarks>
        [ViewVariables]
        public bool CanRun => _powerReceiver is null || _powerReceiver.Powered;

        private ApcPowerReceiverComponent? _powerReceiver;

        protected override void Initialize()
        {
            base.Initialize();
            Id = ServerCount++;
            EntitySystem.Get<ResearchSystem>()?.RegisterServer(this);
            Database = Owner.EnsureComponent<TechnologyDatabaseComponent>();
            IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out _powerReceiver);
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();
            EntitySystem.Get<ResearchSystem>()?.UnregisterServer(this);
        }

        public bool CanUnlockTechnology(TechnologyPrototype technology)
        {
            if (Database == null)
                return false;

            if (!Database.CanUnlockTechnology(technology) ||
                _points < technology.RequiredPoints ||
                Database.IsTechnologyUnlocked(technology))
                return false;

            return true;
        }

        /// <summary>
        ///     Unlocks a technology, but only if there are enough research points for it.
        ///     If there are, it subtracts the amount of points from the total.
        /// </summary>
        /// <param name="technology"></param>
        /// <returns></returns>
        public bool UnlockTechnology(TechnologyPrototype technology)
        {
            if (!CanUnlockTechnology(technology)) return false;
            var result = Database?.UnlockTechnology(technology) ?? false;
            if (result)
                _points -= technology.RequiredPoints;
            return result;
        }

        /// <summary>
        ///     Check whether a technology is unlocked or not.
        /// </summary>
        /// <param name="technology"></param>
        /// <returns></returns>
        public bool IsTechnologyUnlocked(TechnologyPrototype technology)
        {
            return Database?.IsTechnologyUnlocked(technology) ?? false;
        }

        /// <summary>
        ///     Registers a remote client on this research server.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool RegisterClient(ResearchClientComponent client)
        {
            if (client is ResearchPointSourceComponent source)
            {
                if (PointSources.Contains(source)) return false;
                PointSources.Add(source);
                source.Server = this;
                return true;
            }

            if (Clients.Contains(client)) return false;
            Clients.Add(client);
            client.Server = this;
            return true;
        }

        /// <summary>
        ///     Unregisters a remote client from this server.
        /// </summary>
        /// <param name="client"></param>
        public void UnregisterClient(ResearchClientComponent client)
        {
            if (client is ResearchPointSourceComponent source)
            {
                PointSources.Remove(source);
                return;
            }

            Clients.Remove(client);
        }

        public void Update(float frameTime)
        {
            if (!CanRun) return;
            _timer += frameTime;
            if (_timer < 1f) return;
            _timer = 0f;
            _points += PointsPerSecond;
        }
    }
}
