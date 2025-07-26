using Content.Shared.Doors.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Doors.Components;

/// <summary>
/// This is used for a condition door that allows entry only through a single side.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedTurnstileSystem))]
public sealed partial class TurnstileComponent : Component
{
    /// <summary>
    /// A whitelist of the things this turnstile can choose to block or let through.
    /// Things not in this whitelist will be ignored by default.
    /// </summary>
    [DataField]
    public EntityWhitelist? ProcessWhitelist;

    /// <summary>
    /// The next time at which the resist message can show.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextResistTime;

    /// <summary>
    /// Maintained hashset of entities currently passing through the turnstile.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> CollideExceptions = new();

    /// <summary>
    /// default state of the turnstile sprite.
    /// </summary>
    [DataField]
    public string DefaultState = "turnstile";

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
    public SoundSpecifier? TurnSound = new SoundPathSpecifier("/Audio/Items/ratchet.ogg", AudioParams.Default.WithVolume(-6));

    /// <summary>
    /// Sound to play when the turnstile denies entry
    /// </summary>
    [DataField]
    public SoundSpecifier? DenySound = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg")
    {
        Params = new()
        {
            Volume = -7,
        },
    };
}

[Serializable, NetSerializable]
public enum TurnstileVisualLayers : byte
{
    Base
}
