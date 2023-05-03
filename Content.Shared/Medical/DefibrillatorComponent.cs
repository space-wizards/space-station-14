using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical;

/// <summary>
/// This is used for defibrillators; a machine that shocks a dead
/// person back into the world of the living.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class DefibrillatorComponent : Component
{
    /// <summary>
    /// Whether or not it's turned on and able to be used.
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    /// <summary>
    /// The time at which the zap cooldown will be completed
    /// </summary>
    [DataField("nextZapTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextZapTime = TimeSpan.Zero;

    /// <summary>
    /// The minimum time between zaps
    /// </summary>
    [DataField("zapDelay"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ZapDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How much damage is healed from getting zapped.
    /// </summary>
    [DataField("zapHeal", required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ZapHeal = default!;

    /// <summary>
    /// The electrical damage from getting zapped.
    /// </summary>
    [DataField("zapDamage"), ViewVariables(VVAccess.ReadWrite)]
    public int ZapDamage = 5;

    /// <summary>
    /// How long the victim will be electrocuted after getting zapped.
    /// </summary>
    [DataField("writheDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan WritheDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long the doafter for zapping someone takes
    /// </summary>
    /// <remarks>
    /// This is synced with the audio; do not change one but not the other.
    /// </remarks>
    [DataField("doAfterDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The sound when someone is zapped.
    /// </summary>
    [DataField("zapSound")]
    public SoundSpecifier? ZapSound;

    /// <summary>
    /// The sound when the defib is powered on.
    /// </summary>
    [DataField("powerOnSound")]
    public SoundSpecifier? PowerOnSound;

    [DataField("powerOffSound")]
    public SoundSpecifier? PowerOffSound;

    [DataField("chargeSound")]
    public SoundSpecifier? ChargeSound;

    [DataField("failureSound")]
    public SoundSpecifier? FailureSound;

    [DataField("successSound")]
    public SoundSpecifier? SuccessSound;

    [DataField("readySound")]
    public SoundSpecifier? ReadySound;
}

[Serializable, NetSerializable]
public enum DefibrillatorVisuals : byte
{
    Ready
}

[Serializable, NetSerializable]
public sealed class DefibrillatorZapDoAfterEvent : SimpleDoAfterEvent
{

}
