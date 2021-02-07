using System;
using Content.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;

namespace Content.Client.GameObjects.EntitySystems
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
