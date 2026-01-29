using Content.Shared.Prying.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Prying.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(PryingSystem))]
[AutoGenerateComponentState]
public sealed partial class PryingComponent : Component
{
    /// <summary>
    /// Whether the entity can pry open powered doors
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public PryStrength Strength = PryStrength.Strong;

    /// <summary>
    /// Modifier on the prying time.
    /// Lower values result in more time.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
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
    [AutoNetworkedField]
    public bool Enabled = true;
}

/// <summary>
/// Raised directed on an entity before prying it.
/// Cancel to stop the entity from being pried open.
/// </summary>
[ByRefEvent]
public record struct BeforePryEvent(EntityUid User, PryStrength Strength)
{
    public readonly EntityUid User = User;

    /// <summary>
    /// Whether prying should be allowed even if whatever is being pried is powered.
    /// </summary>
    public readonly PryStrength Strength = Strength;

    public string? Message;

    /// <summary>
    /// Set to false if prying should be disallowed
    /// If this is set to false, any remaining handlers should be skipped
    /// </summary>
    public bool CanPry = true;
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
    public readonly TimeSpan BaseTime = TimeSpan.FromSeconds(5);

    public GetPryTimeModifierEvent(EntityUid user, TimeSpan baseTime)
    {
        User = user;
        BaseTime = baseTime;
    }
}

public enum PryStrength : byte
{
    Weak, // Bare hands
    Strong, // Basic tool like a crowbar
    Powered, // Can pry powered devices like Jaws of Life
    Forced, // Can pry bolts, currently only zombies and some animals
}
