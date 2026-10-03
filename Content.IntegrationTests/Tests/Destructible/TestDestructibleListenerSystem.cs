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

    [SubscribeLocalEvent]
    public void AddThresholdsToList(EntityUid _, DestructibleComponent comp, DamageThresholdReached args)
    {
        ThresholdsReached.Add(args);
    }

    [SubscribeLocalEvent]
    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        ThresholdsReached.Clear();
    }
}
