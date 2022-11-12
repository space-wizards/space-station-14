using Content.Shared.CombatMode.Pacification;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles enforced pacifism.
/// </summary>
public sealed class PacifistSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        foreach (var comp in EntityQuery<PacifistComponent>())
        {
            EnsureComp<PacifiedComponent>(comp.Owner); // It's a status effect so just enforce it.
        }
    }
}
