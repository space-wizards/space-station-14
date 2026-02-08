using Content.Server.Antag.Components;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Server.Antag.Systems;

/// <summary>
/// Makes entities with <see cref="AutoGameRuleAntagComponent"/> a antagonist either immediately if they have a mind or when a mind is added and force gameRule.
/// </summary>
public sealed class AutoGameRuleAntagSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoGameRuleAntagComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<AutoGameRuleAntagComponent> ent, ref MindAddedMessage args)
    {
        if (!_player.TryGetSessionById(args.Mind.Comp.UserId, out var session))
            return;

        _antag.ForceMakeAntag<AutoGameRuleAntagComponent>(session, ent.Comp.GameRule);
    }
}
