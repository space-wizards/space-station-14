using Content.Shared._Starlight.Humanoid.Markings;
using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Wagging;

namespace Content.Shared._Starlight.Wagging;

public sealed class StarlightWaggingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly StarlightMarkingSystem _starlightMarking = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WaggingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WaggingComponent, MarkingsUpdateEvent>(OnMarkingsUpdate);
    }

    private void OnMapInit(Entity<WaggingComponent> ent, ref MapInitEvent args) => UpdateAction(ent);

    private void OnMarkingsUpdate(Entity<WaggingComponent> ent, ref MarkingsUpdateEvent args)
    {
        // SpawnRandomHumanoid creates uninitialized entities and raises this before init
        if (!ent.Comp.Running)
            return;

        UpdateAction(ent);
    }

    private void UpdateAction(Entity<WaggingComponent> ent)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid)) return;
        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings)) return;

        foreach (var marking in markings)
        {
            if (!_starlightMarking.TryGetWaggingId(marking.MarkingId, out _)) continue;
            if (!_actions.GetAction(ent.Comp.ActionEntity).HasValue)
                _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
        }
    }
}