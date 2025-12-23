using Content.Server.GameTicking.Rules;
using Content.Shared.NukeOps;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.NukeOps;

/// <summary>
/// Used with NukeOps game rule to send war declaration announcement
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(WarDeclaratorSystem), typeof(NukeopsRuleSystem))]
public sealed partial class WarDeclaratorComponent : Component
{
    /// <summary>
    /// Custom war declaration message. If empty, use default.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public string Message = string.Empty;

    /// <summary>
    /// Permission to customize message text
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool AllowEditingMessage = true;

    /// <summary>
    /// War declaration text color
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public Color Color = Color.Red;

    /// <summary>
    /// War declaration sound file path
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Announcements/war.ogg");

    /// <summary>
    /// Fluent ID for the declaration sender title
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public LocId SenderTitle = "comms-console-announcement-title-nukie";

    /// <summary>
    /// Time allowed for declaration of war
    /// </summary>
    [DataField]
    public float WarDeclarationDelay = 6.0f;

    /// <summary>
    /// Changes the alert level on all stations with the nuke disk
    /// if null, the alert level will not change.
    /// </summary>
    public string? SetAlertlevel = "gamma";

    /// <summary>
    /// How many seconds after the declaration of war, the alert level will change to gamma
    /// </summary>
    [DataField]
    public int AlertlevelDelay = 10;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan DisableAt;

    /// <summary>
    /// How long the shuttle will be disabled for
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan ShuttleDisabledTime;


    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan AlertlevelTime;

    [DataField]
    public WarConditionStatus? CurrentStatus;
}

[ByRefEvent]
public record struct WarDeclaredEvent(WarConditionStatus? Status, Entity<WarDeclaratorComponent> DeclaratorEntity);
