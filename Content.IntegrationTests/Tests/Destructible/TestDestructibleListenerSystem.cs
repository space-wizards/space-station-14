using System.Collections.Generic;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Tests.Destructible;

/// <summary>
/// This is just a system for testing destructible thresholds. Whenever any threshold is reached, this will add that
/// threshold to a list for checking during testing.
/// </summary>
[Reflect(false)]
public sealed partial class TestDestructibleListenerSystem : EntitySystem
{
    public readonly List<DamageThresholdReached> ThresholdsReached = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DestructibleComponent, DamageThresholdReached>(AddThresholdsToList);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void AddThresholdsToList(Entity<DestructibleComponent> ent, ref DamageThresholdReached args)
    {
        ThresholdsReached.Add(args);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        ThresholdsReached.Clear();
    }
}
