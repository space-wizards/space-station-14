using System.Collections.Generic;
using Content.Server.Destructible;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Destructible
{
    /// <summary>
    ///     This is just a system for testing destructible thresholds. Whenever any threshold is reached, this will add that
    ///     threshold to a list for checking during testing.
    /// </summary>
    public class TestDestructibleListenerSystem : EntitySystem
    {
        public readonly List<DamageThresholdReached> ThresholdsReached = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DestructibleComponent, DamageThresholdReached>(AddThresholdsToList);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        public void AddThresholdsToList(EntityUid _, DestructibleComponent comp, DamageThresholdReached args)
        {
            ThresholdsReached.Add(args);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            ThresholdsReached.Clear();
        }
    }
}
