using Content.Server.Objectives.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Tag;

namespace Content.Server.Objectives.Systems;

public sealed class DrainStationPetsConditionSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrainStationPetsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, DrainStationPetsConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var total = 0;
        var dead = 0;

        var query = EntityQueryEnumerator<TagComponent, MobStateComponent>();
        while (query.MoveNext(out var petUid, out _, out var state))
        {
            if (!_tag.HasTag(petUid, "StationPet"))
                continue;

            total++;
            if (state.CurrentState == MobState.Dead)
                dead++;
        }

        args.Progress = total == 0 ? 1f : (float) dead / total;
    }
}
