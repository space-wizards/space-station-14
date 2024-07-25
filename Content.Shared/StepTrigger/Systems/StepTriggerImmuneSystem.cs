using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Content.Shared.StepTrigger.Components;

namespace Content.Shared.StepTrigger.Systems;

public sealed class StepTriggerImmuneSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StepTriggerComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<ClothingRequiredStepTriggerComponent, StepTriggerAttemptEvent>(OnStepTriggerClothingAttempt);
        SubscribeLocalEvent<ClothingRequiredStepTriggerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnStepTriggerAttempt(EntityUid uid, StepTriggerComponent component, ref StepTriggerAttemptEvent args)
    {
        if (!TryComp<StepTriggerImmuneComponent>(args.Tripper, out var comp))
            return;

        if (_whitelistSystem.IsWhitelistFailOrNull(comp.Whitelist, args.Source))
            return;

        args.Cancelled = true;
    }

    private void OnStepTriggerClothingAttempt(EntityUid uid, ClothingRequiredStepTriggerComponent component, ref StepTriggerAttemptEvent args)
    {
        if (_inventory.TryGetInventoryEntity<ClothingRequiredStepTriggerImmuneComponent>(args.Tripper, out _))
        {
            args.Cancelled = true;
        }
    }

    private void OnExamined(EntityUid uid, ClothingRequiredStepTriggerComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("clothing-required-step-trigger-examine"));
    }
}
