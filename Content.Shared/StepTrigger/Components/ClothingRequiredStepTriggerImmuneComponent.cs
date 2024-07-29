using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
/// This is used for cancelling step trigger events if the user is wearing clothing in a valid slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(StepTriggerImmuneSystem))]
public sealed partial class ClothingRequiredStepTriggerImmuneComponent : Component, IClothingSlots
{
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.FEET;
}
