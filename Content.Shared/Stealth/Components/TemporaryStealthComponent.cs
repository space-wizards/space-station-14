using Robust.Shared.GameStates;

namespace Content.Shared.Stealth.Components;

/// <summary>
/// Some systems can make an item temporarily invisible.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TemporaryStealthComponent : Component
{
    /// <summary>
    /// Time to enter invisibility.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FadeInTime = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// Time to come out of invisibility
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FadeOutTime = TimeSpan.FromSeconds(3f);

    /// <summary>
    /// Time in a state of invisibility. Does not include entry and exit times.
    /// If you put 1 second of invisibility on an entity, it will first enter it for 2 seconds,
    /// and then separately exit it for another 3 seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.Zero;

    /// <summary>
    /// The target visibility level that the entity will aim for while under this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TargetVisibility = -1f;

    /// <summary>
    /// The moment of imposition of this component, is calculated automatically by the system.
    /// The invisibility level timings are calculated based on the shift from this moment of time.
    /// </summary>
    
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan StartTime = TimeSpan.Zero;

    /// <summary>
    /// If the entity did not have a <see cref="StealthComponent"/> at the time the component was
    /// received <see cref="TemporaryStealthComponent"/>, StealthComponent will be removed when this component removed
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RemoveStealth = false;
}
