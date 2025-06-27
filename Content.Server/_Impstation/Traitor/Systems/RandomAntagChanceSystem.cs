using System;
using Content.Server.Antag;
using Content.Server._Impstation.Traitor.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Traitor.Systems;

/// <summary>
/// Makes entities with <see cref="RandomAntagChanceComponent"/> the defined antag at a set random chance.
/// </summary>
public sealed class RandomAntagChanceSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAntagChanceComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, RandomAntagChanceComponent comp, MindAddedMessage args)
    {
        if (!_player.TryGetSessionById(args.Mind.Comp.UserId, out var session))
            return;

        var random = new Random();
        if (random.NextDouble() > comp.Chance)
            return;

        _antag.ForceMakeAntag<RandomAntagChanceComponent>(session, comp.Profile);
    }
}
