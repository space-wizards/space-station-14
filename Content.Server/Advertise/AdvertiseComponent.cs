using System.Collections.Generic;
using System.Threading;
using Content.Server.Advertisements;
using Content.Server.Interfaces.Chat;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class AdvertiseComponent : Component
    {
        public override string Name => "Advertise";

        private CancellationTokenSource _cancellationSource = new();

        /// <summary>
        /// Minimum time to wait before saying a new ad, in seconds. Has to be larger than or equal to 1.
        /// </summary>
        [ViewVariables]
        [DataField("minWait")]
        private int MinWait { get; } = 480; // 8 minutes

        /// <summary>
        /// Maximum time to wait before saying a new ad, in seconds. Has to be larger than or equal
        /// to <see cref="MinWait"/>
        /// </summary>
        [ViewVariables]
        [DataField("maxWait")]
        private int MaxWait { get; } = 600; // 10 minutes

        [DataField("pack")]
        private string PackPrototypeId { get; } = string.Empty;

        private List<string> _advertisements = new();

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.Resolve<IPrototypeManager>().TryIndex(PackPrototypeId, out AdvertisementsPackPrototype? packPrototype);

            // Load advertisements pack
            if (string.IsNullOrEmpty(PackPrototypeId) || packPrototype == null)
            {
                // If there is no pack, log a warning and don't start timer
                Logger.Warning($"{Owner} has {Name}Component but no advertisments pack.");
                return;
            }

            _advertisements = packPrototype.Advertisements;

            // Do not start timer if advertisement list is empty
            if (_advertisements.Count == 0)
            {
                // If no advertisements could be loaded, log a warning and don't start timer
                Logger.Warning($"{Owner} tried to load advertisements pack without ads.");
                return;
            }

            // Throw exception if MinWait is smaller than 1.
            if (MinWait < 1)
            {
                throw new PrototypeLoadException($"{Owner} has illegal minWait for {Name}Component: {MinWait}.");
            }

            // Throw exception if MinWait larger than MaxWait.
            if (MinWait > MaxWait)
            {
                throw new PrototypeLoadException($"{Owner} should have minWait greater than or equal to maxWait for {Name}Component.");
            }

            // Start timer at initialization, without MinWait bound
            RefreshTimer(false);
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
            IRobustRandom random = IoCManager.Resolve<IRobustRandom>();
            IChatManager chatManager = IoCManager.Resolve<IChatManager>();

            // Say advertisement
            chatManager.EntitySay(Owner, Loc.GetString(random.Pick(_advertisements)));

            // Refresh timer to repeat cycle
            RefreshTimer();
        }

        /// <summary>
        /// Refresh cancellation token and spawn new timer with random wait between <see cref="MinWait"/>
        /// and <see cref="MaxWait"/>.
        /// <param name="minBound">
        /// Whether <see cref="MinWait"/> should be used to have a minimum waiting time.
        /// </param>
        /// </summary>
        private void RefreshTimer(bool minBound = true)
        {
            // Generate new source
            _cancellationSource.Cancel();
            _cancellationSource.Dispose();
            _cancellationSource = new CancellationTokenSource();

            // Generate random wait time, then create timer
            IRobustRandom random = IoCManager.Resolve<IRobustRandom>();
            var wait = minBound ? random.Next(MinWait * 1000, MaxWait * 1000) : random.Next(MaxWait * 1000);
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
            // Restart timer, without minBound
            RefreshTimer(false);
        }
    }
}
