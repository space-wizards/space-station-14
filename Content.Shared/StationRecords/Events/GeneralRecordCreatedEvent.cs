using Content.Shared.Preferences;

namespace Content.Shared.StationRecords.Events;

/// <summary>
///     Event raised after the player's general profile is created.
///     Systems that modify records on a station would have more use
///     listening to this event, as it contains the character's record key.
///     Also stores the general record reference, to save some time.
/// </summary>
[ByRefEvent]
public readonly record struct GeneralRecordCreatedEvent(
    StationRecordKey Key,
    GeneralStationRecord Record,
    HumanoidCharacterProfile Profile) : IStationRecordEvent
{
    public EntityUid Station => Key.OriginStation;

    public readonly GeneralStationRecord Record = Record;

    /// <summary>
    /// Profile for the related player. This is so that other systems can get further information
    ///     about the player character.
    ///     Optional - other systems should anticipate this.
    /// </summary>
    public readonly HumanoidCharacterProfile Profile = Profile;
}
