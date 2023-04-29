using Content.Shared.Salvage;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Salvage.Expeditions;

/// <summary>
/// Designates this entity as holding a salvage expedition.
/// </summary>
[RegisterComponent]
public sealed class SalvageExpeditionComponent : Component
{
    public SalvageMissionParams MissionParams = default!;

    /// <summary>
    /// Where the dungeon is located for initial announcement.
    /// </summary>
    [DataField("dungeonLocation")]
    public Vector2 DungeonLocation = Vector2.Zero;

    /// <summary>
    /// When the expeditions ends.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EndTime;

    /// <summary>
    /// Station whose mission this is.
    /// </summary>
    [ViewVariables, DataField("station")]
    public EntityUid Station;

    [ViewVariables] public bool Completed = false;

    [ViewVariables(VVAccess.ReadWrite), DataField("stage")]
    public ExpeditionStage Stage = ExpeditionStage.Added;

    /// <summary>
    /// Countdown audio stream.
    /// </summary>
    public IPlayingAudioStream? Stream = null;

    /// <summary>
    /// Sound that plays when the mission end is imminent.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("sound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Misc/tension_session.ogg")
    {
        Params = AudioParams.Default.WithVolume(-15),
    };
}

public enum ExpeditionStage : byte
{
    Added,
    Running,
    Countdown,
    MusicCountdown,
    FinalCountdown,
}
