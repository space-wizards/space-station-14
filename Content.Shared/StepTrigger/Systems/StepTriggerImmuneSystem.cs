using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Components;

namespace Content.Shared.StepTrigger.Systems;

public sealed class StepTriggerImmuneSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PreventableStepTriggerComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<PreventableStepTriggerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnStepTriggerAttempt(Entity<PreventableStepTriggerComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (HasComp<ProtectedFromStepTriggersComponent>(args.Tripper) || _inventory.TryGetInventoryEntity<ProtectedFromStepTriggersComponent>(args.Tripper, out _))
        {
            args.Cancelled = true;
        }
    }

    private void OnExamined(EntityUid uid, PreventableStepTriggerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("clothing-required-step-trigger-examine"));
    }
}
