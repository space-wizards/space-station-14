using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Tag;

namespace Content.Shared.StepTrigger.Systems;

public sealed class ShoesRequiredStepTriggerSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ShoesRequiredStepTriggerComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ShoesRequiredStepTriggerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnStepTriggerAttempt(EntityUid uid, ShoesRequiredStepTriggerComponent component, ref StepTriggerAttemptEvent args)
    {
        // checks if the entity itself is the one with the component
        if (HasComp<ShoesRequiredStepTriggerImmuneComponent>(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        // go through all equipped items, checks if item is equipped in shoe slot or has shoe immmune component
        if (_inventory.TryGetContainerSlotEnumerator(args.Tripper, out var containerSlotEnumerator, SlotFlags.All & ~SlotFlags.POCKET))
        {
            while (containerSlotEnumerator.NextItem(out var item, out var slot))
            {
                if (HasComp<ShoesRequiredStepTriggerImmuneComponent>(item))
                {
                    args.Cancelled = true;
                    return;
                }
            }
        }
    }

    private void OnExamined(EntityUid uid, ShoesRequiredStepTriggerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("shoes-required-step-trigger-examine"));
    }
}
