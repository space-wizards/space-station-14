using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles progress for <see cref="KillAllHeadsConditionComponent"/>:
/// complete once every command staff member is dead (or none remain).
/// </summary>
public sealed class KillAllHeadsConditionSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillAllHeadsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<KillAllHeadsConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var total = 0;
        var dead = 0;

        var query = EntityQueryEnumerator<CommandStaffComponent>();
        while (query.MoveNext(out var head, out _))
        {
            total++;
            if (_mobState.IsDead(head))
                dead++;
        }

        // Gibbed or deleted heads drop out of the query entirely, so an empty
        // query means the dungeon has been well and truly cleared.
        args.Progress = total == 0 ? 1f : (float) dead / total;
    }
}
