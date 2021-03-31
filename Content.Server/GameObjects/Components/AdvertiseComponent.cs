using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Server.Advertisements;
using Content.Server.Interfaces.Chat;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class AdvertiseComponent : Component
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Advertise";

        private CancellationTokenSource _cancellationSource = new();

        /// <summary>
        /// Minimum time to wait before saying a new ad, in ms.
        /// </summary>
        [field: DataField("minWait")]
        private int MinWait { get; } = 40000;

        /// <summary>
        /// Maximum time to wait before saying a new ad, in ms.
        /// </summary>
        [field: DataField("maxWait")]
        private int MaxWait { get; } = 80000;

        [field: DataField("pack")]
        private string PackPrototypeId { get; } = string.Empty;

        private List<string> _advertisements = new();

        public override void Initialize()
        {
            base.Initialize();

            _prototypeManager.TryIndex(PackPrototypeId, out AdvertisementsPackPrototype? packPrototype);

            // Load advertisements pack
            if (string.IsNullOrEmpty(PackPrototypeId) || packPrototype == null)
            {
                // If there is no pack, log a warning and remove the component
                Logger.Warning($"{Owner} has {Name} Component but no advertisments pack.");
                Owner.RemoveComponent<AdvertiseComponent>();
                return;
            }

            _advertisements = packPrototype.Advertisements.ToList();

            // Do not start timer if advertisement list is empty
            if (_advertisements.Count == 0)
            {
                // If no advertisements could be loaded, log a warning and remove component
                Logger.Log(LogLevel.Warning, Owner.Name + " tried to load advertisements pack without ads.");
                Owner.RemoveComponent<AdvertiseComponent>();
                return;
            }

            // Start timer at initialization
            RefreshTimer();
        }

        public override void OnRemove()
        {
            _cancellationSource.Cancel();
            _cancellationSource.Dispose();

            base.OnRemove();
        }

        /// <summary>
        /// Say advertisement and restart timer.
        /// </summary>
        private void SayAndRefresh()
        {
            // Say advertisement
            _chatManager.EntitySay(Owner, _random.Pick(_advertisements));

            // Refresh timer to repeat cycle
            RefreshTimer();
        }

        /// <summary>
        /// Refresh cancellation token and spawn new timer with random wait between <see cref="MinWait"/>
        /// and <see cref="MaxWait"/>.
        /// </summary>
        private void RefreshTimer()
        {
            // Generate new source
            _cancellationSource.Cancel();
            _cancellationSource.Dispose();
            _cancellationSource = new CancellationTokenSource();

            // Generate random wait time, then create timer
            var wait = _random.Next(MinWait, MaxWait);
            Owner.SpawnTimer(wait, SayAndRefresh, _cancellationSource.Token);
        }

        /// <summary>
        /// Pause the advertising until <see cref="Resume"/> is called.
        /// </summary>
        public void Pause()
        {
            // Cancel current timer
            _cancellationSource.Cancel();
        }

        /// <summary>
        /// Resume the advertising after pausing.
        /// </summary>
        public void Resume()
        {
            // Restart timer
            RefreshTimer();
        }
    }
}
