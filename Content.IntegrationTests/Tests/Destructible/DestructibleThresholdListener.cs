using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests.Destructible
{
    /// <summary>
    ///     This is just a system for testing destructible thresholds. Whenever any threshold is reached, this will add that
    ///     threshold to a list for checking during testing.
    /// </summary>
    public class DestructibleThresholdListenerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TestDestructibleComponent, DamageChangedEvent>(CheckAndAddToList);
        }

        public List<DestructibleThresholdReached> ThresholdsReached = new();

        public void CheckAndAddToList(EntityUid _, TestDestructibleComponent component, DamageChangedEvent args)
        {
            var destructible = EntitySystemManager.GetEntitySystem<DestructibleSystem>();

            foreach (var threshold in component.Thresholds)
            {
                if (threshold.Reached(args.Damageable, destructible))
                {
                    // Execute
                    threshold.Execute(component.Owner, destructible);

                    // Add to a public list, so that the integration tests can check what thresholds were reached.
                    ThresholdsReached.Add(new DestructibleThresholdReached(component, threshold));
                }
            }
        }
    }

    public struct DestructibleThresholdReached
    {
        public TestDestructibleComponent Parent { get; }

        public Threshold Threshold { get; }

        public DestructibleThresholdReached(TestDestructibleComponent parent, Threshold threshold)
        {
            Parent = parent;
            Threshold = threshold;
        }
    }

    // Avoid duplicate subscription errors Yes this is probably a shitty way of fixing this, but its good enough for
    // integration tests.
    [RegisterComponent]
    public class TestDestructibleComponent : DestructibleComponent
    {
        public override string Name => "TestDestructible";
    }
}
