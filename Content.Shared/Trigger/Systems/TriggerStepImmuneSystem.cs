using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Trigger.Components.StepTriggers;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerStepImmuneSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerPreventableStepTriggerComponent, TriggerStepAttemptEvent>(OnStepTriggerClothingAttempt);
        SubscribeLocalEvent<TriggerPreventableStepTriggerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnStepTriggerClothingAttempt(Entity<TriggerPreventableStepTriggerComponent> ent, ref TriggerStepAttemptEvent args)
    {
        if (HasComp<TriggerProtectedFromStepTriggersComponent>(args.Tripper) || _inventory.TryGetInventoryEntity<TriggerProtectedFromStepTriggersComponent>(args.Tripper, out _))
        {
            args.Cancelled = true;
        }
    }

    private void OnExamined(EntityUid uid, TriggerPreventableStepTriggerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("clothing-required-step-trigger-examine"));
    }
}
