using Content.Shared.Inventory;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an entity to check if it has a max contact slowdown.
/// </summary>
[ByRefEvent]
public record struct GetSpeedModifierContactCapEvent() : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;

    public float MaxSprintSlowdown = 0f;

    public float MaxWalkSlowdown = 0f;

    public void SetIfMax(float valueSprint, float valueWalk)
    {
        MaxSprintSlowdown = MathF.Max(MaxSprintSlowdown, valueSprint);
        MaxWalkSlowdown = MathF.Max(MaxWalkSlowdown, valueWalk);
    }
}
