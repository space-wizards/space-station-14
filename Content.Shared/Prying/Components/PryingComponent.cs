using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Prying.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PryingComponent : Component
{
    /// <summary>
    /// Whether the entity can pry open powered doors
    /// </summary>
    [DataField("pryPowered")]
    public bool PryPowered = false;

    /// <summary>
    /// Whether the tool can bypass certain restrictions when prying.
    /// For example door bolts.
    [DataField("force")]
    public bool Force = false;
    /// <summary>
    /// Modifier on the prying time.
    /// Lower values result in more time.
    /// </summary>
    [DataField("speedModifier")]
    public float SpeedModifier = 1.0f;

    /// <summary>
    /// What sound to play when prying is finished.
    /// </summary>
    [DataField("useSound")]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");

    /// <summary>
    /// Whether the entity can currently pry things.
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

    public readonly bool Force;

    public BeforePryEvent(EntityUid user, bool pry_powered, bool force)
    {
        User = user;
        PryPowered = pry_powered;
        Force = force;
    }

}

public sealed class AfterPryEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public AfterPryEvent(EntityUid user)
    {
        User = user;
    }
}

/// <summary>
/// Raised to determine how long the door's pry time should be modified by.
/// Multiply PryTimeModifier by the desired amount.
/// </summary>
public sealed partial class GetPryTimeModifierEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public float PryTimeModifier = 1.0f;
    public float BaseTime = 5.0f;

    public GetPryTimeModifierEvent(EntityUid user)
    {
        User = user;
    }
}

