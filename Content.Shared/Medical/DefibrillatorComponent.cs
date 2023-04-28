using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical;

/// <summary>
/// This is used for defibrillators; a machine that shocks a dead
/// person back into the world of the living.
/// </summary>
[RegisterComponent]
public sealed class DefibrillatorComponent : Component
{
    /// <summary>
    /// Whether or not it's turned on and able to be used.
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    /// <summary>
    /// Whether or not the cooldown after zapping someone has ended.
    /// </summary>
    [DataField("cooldownEnded"), ViewVariables(VVAccess.ReadWrite)]
    public bool CooldownEnded = true;

    [DataField("nextShockTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextShockTime = TimeSpan.Zero;

    [DataField("shockDelay"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShockDelay = TimeSpan.FromSeconds(5);

    [DataField("zapHeal", required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ZapHeal = default!;

    [DataField("zapDamage"), ViewVariables(VVAccess.ReadWrite)]
    public int ZapDamage = 5;

    [DataField("writheDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan WritheDuration = TimeSpan.FromSeconds(3);

    [DataField("doAfterDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(3);

    [DataField("zapSound")]
    public SoundSpecifier? ZapSound;

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
