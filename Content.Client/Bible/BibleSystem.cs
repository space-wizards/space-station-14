using Content.Shared.Bible;
using Content.Shared.Bible.Components;

namespace Content.Client.Bible;

public sealed class BibleSystem : SharedBibleSystem
{
    protected override void AttemptSummon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent? position)
    {
        // No logic, only server side.
    }
}
