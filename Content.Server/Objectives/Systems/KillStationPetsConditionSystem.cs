using Content.Server.Objectives.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Systems;

public sealed partial class KillStationPetsConditionSystem : EntitySystem
{
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly ProtoId<TagPrototype> StationPetTag = "StationPet";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillStationPetsConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<KillStationPetsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(Entity<KillStationPetsConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // Snapshot the pets that exist right now so ones that later get deleted
        // outright (e.g. gibbed) still count towards the objective.
        ent.Comp.Pets.Clear();

        var query = EntityQueryEnumerator<TagComponent, MobStateComponent>();
        while (query.MoveNext(out var petUid, out _, out _))
        {
            if (_tag.HasTag(petUid, StationPetTag))
                ent.Comp.Pets.Add(petUid);
        }

        // No pets aboard, nothing to hunt.
        if (ent.Comp.Pets.Count == 0)
            args.Cancelled = true;
    }

    private void OnGetProgress(Entity<KillStationPetsConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var killed = 0;

        foreach (var pet in ent.Comp.Pets)
        {
            if (Deleted(pet) || _mobState.IsDead(pet))
                killed++;
        }

        args.Progress = ent.Comp.Pets.Count == 0 ? 1f : (float) killed / ent.Comp.Pets.Count;
    }
}
