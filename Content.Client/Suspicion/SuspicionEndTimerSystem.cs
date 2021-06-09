using System;
using Content.Shared.Suspicion;
using Robust.Shared.GameObjects;

namespace Content.Client.Suspicion
{
    public sealed class SuspicionEndTimerSystem : EntitySystem
    {
        public TimeSpan? EndTime { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<SuspicionMessages.SetSuspicionEndTimerMessage>(RxTimerMessage);
        }

        private void RxTimerMessage(SuspicionMessages.SetSuspicionEndTimerMessage ev)
        {
            EndTime = ev.EndTime;
        }
    }
}
