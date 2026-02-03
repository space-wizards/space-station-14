using Content.Server.Antag.Components;
using Content.Shared.Antag;
using Content.Shared.Mind.Components;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Systems;

/// <summary>
/// Makes entities with <see cref="AutoAntagComponent"/> a antagonist either immediately if they have a mind or when a mind is added.
/// </summary>
public sealed class AutoAntagSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoAntagComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<AutoAntagComponent> ent, ref MindAddedMessage args)
    {
        if (!_player.TryGetSessionById(args.Mind.Comp.UserId, out var session))
            return;

        if (!_prototypeManager.Resolve<AntagLoadoutPrototype>(ent.Comp.AntagLoadout, out var loadout))
            return;

        _antag.TryMakeNonGameRuleAntag(ent, loadout, ent);
    }
}
