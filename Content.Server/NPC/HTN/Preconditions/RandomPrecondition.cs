using Content.Shared.Interaction;
using Robust.Shared.Random;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class RandomPrecondition : HTNPrecondition
{
    [Dependency] private IRobustRandom _random = default!;

    [DataField]
    public float Chance = 0.5f;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return _random.Prob(Chance);
    }
}
