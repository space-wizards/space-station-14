using Content.Shared.Creatures.SpaceLeech;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Creatures.SpaceLeech;

/// <summary>
/// Hands out the space leech's objectives when a player takes over the ghost role.
/// </summary>
public sealed class SpaceLeechRoleSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private static readonly EntProtoId[] Objectives =
    [
        "SpaceLeechSurviveObjective",
        "SpaceLeechBloodObjective",
        "SpaceLeechPetBloodObjective",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceLeechComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(Entity<SpaceLeechComponent> ent, ref MindAddedMessage args)
    {
        foreach (var objective in Objectives)
        {
            // Guard against duplicates if the same mind leaves and retakes the leech.
            if (_mind.TryFindObjective(args.Mind.Owner, objective, out _))
                continue;

            _mind.TryAddObjective(args.Mind, args.Mind.Comp, objective);
        }
    }
}
