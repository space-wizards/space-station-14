using Content.Server.Nuke;
using Content.Server.StationEvents.Events;
using Robust.Shared.Audio;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(NukeCalibrationRule))]
public sealed partial class NukeCalibrationRuleComponent : Component
{
    /// <summary>
    /// Sound of the announcement to play if automatic disarm of the nuke was unsuccessful.
    /// </summary>
    [DataField]
    public SoundSpecifier AutoDisarmFailedSound = new SoundPathSpecifier("/Audio/Misc/notice1.ogg");
    /// <summary>
    /// Sound of the announcement to play if automatic disarm of the nuke was successful.
    /// </summary>
    [DataField]
    public SoundSpecifier AutoDisarmSuccessSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");

    [DataField]
    public EntityUid AffectedStation;
    /// <summary>
    /// The nuke that was '''calibrated'''.
    /// </summary>
    [DataField]
    public EntityUid AffectedNuke;
    [DataField]
    public float NukeTimer = 170f;
    [DataField]
    public float AutoDisarmChance = 0.5f;
    [DataField]
    public float TimeUntilFirstAnnouncement = 15f;
    [DataField]
    public bool FirstAnnouncementMade = false;
}
