using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Inventory;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <summary>
/// This is used for cancelling preventable step trigger events if the user is wearing clothing in a valid slot or if the user itself has the component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerProtectedFromStepTriggersComponent : BaseTriggerOnXComponent, IClothingSlots
{
    [DataField]
    public SlotFlags Slots { get; set; } = SlotFlags.FEET;
}
