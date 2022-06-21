using Content.Shared.Inventory;
using Content.Shared.Movement.EntitySystems;

namespace Content.Shared.Movement.Events;

/// <summary>
///     Raised on an entity to determine its new movement speed. Any system that wishes to change movement speed
///     should hook into this event and set it then. If you want this event to be raised,
///     call <see cref="MovementSpeedModifierSystem.RefreshMovementSpeedModifiers"/>.
/// </summary>
public sealed class RefreshMovementSpeedModifiersEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

    public float WalkSpeedModifier { get; private set; } = 1.0f;
    public float SprintSpeedModifier { get; private set; } = 1.0f;

    public void ModifySpeed(float walk, float sprint)
    {
        WalkSpeedModifier *= walk;
        SprintSpeedModifier *= sprint;
    }
}