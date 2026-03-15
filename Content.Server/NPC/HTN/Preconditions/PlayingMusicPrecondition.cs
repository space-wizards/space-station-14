using Content.Server.Instruments;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Returns true if the owner is an instrument that is currently playing music.
/// </summary>
public sealed partial class IsPlayingMusicPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<InstrumentComponent>(owner, out var instrument))
            return false;

        return instrument.Playing;
    }
}
