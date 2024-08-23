using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Prying.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PryingComponent : Component
{
    /// <summary>
    /// Whether the entity can pry open powered doors
    /// </summary>
    [DataField]
    public bool PryPowered;

    /// <summary>
    /// Whether the tool can bypass certain restrictions when prying.
    /// For example door bolts.
    /// </summary>
    [DataField]
    public bool Force;
    /// <summary>
    /// Modifier on the prying time.
    /// Lower values result in more time.
    /// </summary>
    [DataField]
    public float SpeedModifier = 1.0f;

    /// <summary>
    /// What sound to play when prying is finished.
    /// </summary>
    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");

    /// <summary>
    /// Whether the entity can currently pry things.
    /// </summary>
    [DataField]
    public bool Enabled = true;
}

/// <summary>
/// Raised directed on an entity before prying it.
/// Cancel to stop the entity from being pried open.
/// </summary>
[ByRefEvent]
public record struct BeforePryEvent(EntityUid User, bool PryPowered, bool Force, bool StrongPry)
{
    public readonly EntityUid User = User;

    /// <summary>
    /// Whether prying should be allowed even if whatever is being pried is powered.
    /// </summary>
    public readonly bool PryPowered = PryPowered;

    /// <summary>
    /// Whether prying should be allowed to go through under most circumstances. (E.g. airlock is bolted).
    /// Systems may still wish to ignore this occasionally.
    /// </summary>
    public readonly bool Force = Force;

    /// <summary>
    /// Whether anything other than bare hands were used. This should only be false if prying is being performed without a prying comp.
    /// </summary>
    public readonly bool StrongPry = StrongPry;

    public string? Message;

    public bool Cancelled;
}

/// <summary>
/// Raised directed on an entity that has been pried.
/// </summary>
[ByRefEvent]
public readonly record struct PriedEvent(EntityUid User)
{
    public readonly EntityUid User = User;
}

/// <summary>
/// Raised to determine how long the door's pry time should be modified by.
/// Multiply PryTimeModifier by the desired amount.
/// </summary>
[ByRefEvent]
public record struct GetPryTimeModifierEvent
{
    public readonly EntityUid User;
    public float PryTimeModifier = 1.0f;
    public float BaseTime = 5.0f;

    public GetPryTimeModifierEvent(EntityUid user)
    {
        User = user;
    }
}

