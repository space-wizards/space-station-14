using Robust.Shared.Audio;

namespace Content.Shared.Doors.Prying.Components;

[RegisterComponent]
public sealed class DoorPryingComponent : Component
{
    /// <summary>
    /// Whether the tool can pry open powered doors
    /// </summary>
    [DataField("pryPowered")]
    public bool PryPowered = false;

    [DataField("speedModifier")]
    public float SpeedModifier = 1.0f;

    [DataField("useSound")]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");
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
/// Raised before the door prying doafter starts.
/// Can be used to add to, or remove to the time it takes to pry open a
/// door.
/// </summary>
sealed class PryTimeModifierEvent : EntityEventArgs
{
    public readonly EntityUid User;

    public float Modifier = 1.0f;

    public PryTimeModifierEvent(EntityUid user)
    {
        User = user;
    }
}
///
/// <summary>
/// Raised to determine how long the door's pry time should be modified by.
/// Multiply PryTimeModifier by the desired amount.
/// </summary>
public sealed class DoorGetPryTimeModifierEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public float PryTimeModifier = 1.0f;

    public DoorGetPryTimeModifierEvent(EntityUid user)
    {
        User = user;
    }
}
