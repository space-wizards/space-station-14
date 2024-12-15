using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CloudEmote;

/// <summary>
/// While entity has this component it is "disabled" by EMP.
/// Add desired behaviour in other systems
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CloudEmoteActiveComponent : Component
{
    /// <summary>
    /// Moment of time when component is removed and entity stops being "disabled"
    /// </summary>
    [DataField("timeLeft", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan DisabledUntil;

    [DataField("effectCoolDown"), ViewVariables(VVAccess.ReadWrite)]
    public float EffectCooldown = 3f;

    [DataField("emote_name"), ViewVariables(VVAccess.ReadWrite)]
    public string EmoteName = "";


    [DataField("phase"), ViewVariables(VVAccess.ReadWrite)]
    public int Phase = -1; // -1 - emote inited, 0 - emote started, 1 - emote in progress, 2 - emote ending

    [DataField("entity"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Emote;

    /// <summary>
    /// When next effect will be spawned
    /// </summary>
    [AutoPausedField]
    public TimeSpan TargetTime = TimeSpan.Zero;
}
