using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Prying.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DoorPryingComponent : Component
{
    /// <summary>
    /// Whether the tool can pry open powered doors
    /// </summary>
    [DataField("pryPowered")]
    public bool PryPowered = false;

    /// <summary>
    /// How is the prying time impacted.
    /// Lower values will result in more time.
    /// </summary>
    [DataField("speedModifier")]
    public float SpeedModifier = 1.0f;

    /// <summary>
    /// Sound to play for prying a door open
    /// </summary>
    [DataField("useSound")]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");

    /// <summary>
    /// Whether the entity with the component can pry open a door.
    /// </summary>
    [DataField("enabled")]
    public bool Enabled = true;
}

/// <summary>
/// Raised before prying open a door.
/// Cancel to stop the door from being pried open
/// </summary>
public sealed class BeforePryEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;

    public readonly bool PryPowered;

    public BeforePryEvent(EntityUid user, bool pry_powered)
    {
        User = user;
        PryPowered = pry_powered;
    }

}

/// <summary>
/// Raised to determine how long the door's pry time should be modified by.
/// Multiply PryTimeModifier by the desired amount.
/// </summary>
public sealed partial class DoorGetPryTimeModifierEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public float PryTimeModifier = 1.0f;

    public DoorGetPryTimeModifierEvent(EntityUid user)
    {
        User = user;
    }
}

/// <summar>
/// Raised to enable or disable prying for a specific entity with the component.
/// </summary>
public sealed partial class DoorPryingToggleEvent : EntityEventArgs
{
    public bool Enabled;

    public DoorPryingToggleEvent(bool enabled)
    {
        Enabled = enabled;
    }
}
