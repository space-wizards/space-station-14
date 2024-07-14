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
/// Uses <c>ItemToggleComponent</c>
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedDefibrillatorSystem)), AutoGenerateComponentPause]
public sealed partial class DefibrillatorComponent : Component
{
    /// <summary>
    /// The time at which the zap cooldown will be completed
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan? NextZapTime;

    /// <summary>
    /// The minimum time between zaps
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ZapDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How much damage is healed from getting zapped.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ZapHeal = default!;

    /// <summary>
    /// The electrical damage from getting zapped.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int ZapDamage = 5;

    /// <summary>
    /// How long the victim will be electrocuted after getting zapped.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan WritheDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long the doafter for zapping someone takes
    /// </summary>
    /// <remarks>
    /// This is synced with the audio; do not change one but not the other.
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(3);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool AllowDoAfterMovement = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool CanDefibCrit = true;

    /// <summary>
    /// The sound when someone is zapped.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ZapSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_charge.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? FailureSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite),]
    public SoundSpecifier? ReadySound = new SoundPathSpecifier("/Audio/Items/Defib/defib_ready.ogg");
}

[Serializable, NetSerializable]
public enum DefibrillatorVisuals : byte
{
    Ready
}

[Serializable, NetSerializable]
public sealed partial class DefibrillatorZapDoAfterEvent : SimpleDoAfterEvent
{
}
