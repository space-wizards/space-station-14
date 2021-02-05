using System.Collections.Generic;
using Content.Server.GameObjects.Components.Destructible;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.IntegrationTests.Tests.Destructible
{
    public class TestThresholdListenerComponent : Component
    {
        public override string Name => "TestThresholdListener";

        public List<DestructibleThresholdReachedMessage> ThresholdsReached { get; } = new();

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case DestructibleThresholdReachedMessage msg:
                    ThresholdsReached.Add(msg);
                    break;
            }
        }
    }
}
