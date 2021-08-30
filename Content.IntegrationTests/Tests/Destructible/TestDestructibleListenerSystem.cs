using Content.Server.Destructible;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests.Destructible
{
    /// <summary>
    ///     This is just a system for testing destructible thresholds. Whenever any threshold is reached, this will add that
    ///     threshold to a list for checking during testing.
    /// </summary>
    public class TestDestructibleListenerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DestructibleComponent, DamageThresholdReached>(AddThresholdsToList);
        }

        public void AddThresholdsToList(EntityUid _, DestructibleComponent comp, DamageThresholdReached args)
        {
            ThresholdsReached.Add(args);
        }

        public List<DamageThresholdReached> ThresholdsReached = new();
    }
}
