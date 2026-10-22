using Content.Shared.Damage.Components;

namespace Content.Shared.Damage.Events;

/// <summary>
/// Raised whenever the <see cref="StaminaComponent.CritThreshold"/> needs to be refreshed.
/// </summary>
[ByRefEvent]
public record struct RefreshStaminaCritThresholdEvent
{
    public float ThresholdValue = 100f;
    public float Modifier = 1f;

    public RefreshStaminaCritThresholdEvent(float thresholdValue)
    {
        ThresholdValue = thresholdValue;
    }
}
