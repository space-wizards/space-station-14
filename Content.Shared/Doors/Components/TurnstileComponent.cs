using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Components;

/// <summary>
/// This is used for a condition door that allows entry only through a single side.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class TurnstileComponent : Component
{
    /// <summary>
    /// A whitelist of the things this turnstile blocks. Anything else (bullets, etc) can fly right through.
    /// </summary>
    [DataField]
    public EntityWhitelist BlockWhitelist = new();

    /// <summary>
    /// The next time this turnstile can attempt to be passed through.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPassTime;

    /// <summary>
    /// The minimum time between passes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PassDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The next time at which the resist message can show.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextResistTime;

    /// <summary>
    /// Maintained hashset of entities currently passing through the turnstile.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> CollideExceptions = new();

    /// <summary>
    /// animation state of the turnstile spinning.
    /// </summary>
    [DataField]
    public string SpinState = "operate";

    /// <summary>
    /// animation state of the turnstile denying entry.
    /// </summary>
    [DataField]
    public string DenyState = "deny";

    /// <summary>
    /// Sound to play when the turnstile admits a mob through.
    /// </summary>
    [DataField]
    public SoundSpecifier? TurnSound = new SoundPathSpecifier("/Audio/Items/ratchet.ogg");

    /// <summary>
    /// Sound to play when the turnstile denies entry
    /// </summary>
    [DataField]
    public SoundSpecifier? DenySound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");
}

[Serializable, NetSerializable]
public enum TurnstileVisualLayers : byte
{
    Base
}
