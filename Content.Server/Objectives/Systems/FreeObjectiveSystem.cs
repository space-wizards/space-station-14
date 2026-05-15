using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class FreeObjectiveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FreeObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    // You automatically greentext, there's not much else to it
    private void OnGetProgress(Entity<FreeObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 1f;
    }
}
