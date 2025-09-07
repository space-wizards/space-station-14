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
    public Dictionary<EntityUid, EntranceMethod> CollideExceptions = new();

    /// <summary>
    /// Maintained dictionary of entities that can enter due to a successful prying DoAfter
    /// The values represent the game time at which the entry will expire.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, TimeSpan> PriedExceptions = new();

    /// <summary>
    /// default state of the turnstile sprite.
    /// </summary>
    [DataField]
    public string DefaultState = "turnstile_idle";

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
    /// animation state of the turnstile granting entry.
    /// </summary>
    [DataField]
    public string GrantedState = "granted";

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

    /// <summary>
    /// This is similar to the POWER wire on traditional airlocks
    /// While this is true, the turnstile may be bypassed by prying and the bolts will not actuate.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SolenoidBypassed;

    /// <summary>
    /// Pry modifier for a turnstile without bypassing the solenoid.
    /// Most anything that can pry powered has a pry speed bonus,
    /// so this default is closer to 6 effectively on e.g. jaws (9 seconds when applied to other default.)
    /// </summary>
    [DataField]
    public float PoweredPryModifier = 9f;

    /// <summary>
    /// Pry modifier for a prying a turnstile from the wrong direction.
    /// </summary>
    [DataField]
    public float WrongDirectionPryModifier = 2f;

    /// <summary>
    /// The amount of time it takes for a successful pry DoAfter to expire
    /// You must enter the turnstile within this amount of time after the DoAfter finishes
    /// </summary>
    [DataField]
    public TimeSpan PryExpirationTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public bool AccessBroken;
}

[Serializable, NetSerializable]
public enum TurnstileVisuals : byte
{
    AccessBroken,
}

[Serializable, NetSerializable]
public enum TurnstileVisualLayers : byte
{
    Base,
    Spinner,
    Indicators,
    BoltIndicators,
}

[Serializable, NetSerializable]
public enum EntranceMethod : byte
{
    Access = 0,
    Pulled,
    ChainPulled,
    Forced,
    AccessBroken,
}
