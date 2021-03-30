using System.Threading;
using Content.Server.Interfaces.Chat;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class AdvertiseComponent : Component
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "Advertise";

        private CancellationTokenSource _cancellationToken = new();
        private const int MinAdWait = 1000;
        private const int MaxAdWait = 5000;

        public override void Initialize()
        {
            base.Initialize();

            AddTimer();
        }

        public override void OnRemove()
        {
            _cancellationToken.Cancel();
            _cancellationToken.Dispose();

            base.OnRemove();
        }

        public void Pause()
        {
            _cancellationToken.Cancel();
        }

        private void Say()
        {
            _chatManager.EntitySay(Owner, "Hi, I'm a vending machine!");

            AddTimer();
        }

        private void AddTimer()
        {
            // If advertising is cancelled, return and do not add a new timer
            if (_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Generate random wait time, then create timer
            var wait = _random.Next(MinAdWait, MaxAdWait);
            Owner.SpawnTimer(wait, Say, _cancellationToken.Token);
        }
    }
}
