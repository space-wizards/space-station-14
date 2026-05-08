using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <summary>
/// This is used for cancelling preventable step trigger events
/// if the user is wearing clothing in a valid slot or if the user itself has the component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerProtectedFromStepTriggersComponent : BaseStepTriggerOnXComponent, IClothingSlots
{
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.FEET;
}
