using System.Collections.Generic;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.Destructible
{
    /// <summary>
    ///     This is just a system for testing destructible thresholds. Whenever any threshold is reached, this will add that
    ///     threshold to a list for checking during testing.
    /// </summary>
    [Reflect(false)]
    public sealed class TestDestructibleListenerSystem : EntitySystem
    {
        public readonly List<SharedDestructibleSystem.DamageThresholdReached> ThresholdsReached = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DestructibleComponent, SharedDestructibleSystem.DamageThresholdReached>(AddThresholdsToList);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        public void AddThresholdsToList(EntityUid _, DestructibleComponent comp, SharedDestructibleSystem.DamageThresholdReached args)
        {
            ThresholdsReached.Add(args);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            ThresholdsReached.Clear();
        }
    }
}
