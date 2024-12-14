using Content.Server.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MeteorSwarmSystem)), AutoGenerateComponentPause]
public sealed partial class MeteorSwarmComponent : Component
{
    [DataField, AutoPausedField]
    public TimeSpan NextWaveTime;

    /// <summary>
    /// We'll send a specific amount of waves of meteors towards the station per ending rather than using a timer.
    /// </summary>
    [DataField]
    public int WaveCounter;

    [DataField]
    public float MeteorVelocity = 10f;

    /// <summary>
    /// If true, meteors will be thrown from all angles instead of from a singular source
    /// </summary>
    [DataField]
    public bool NonDirectional;

    /// <summary>
    /// The announcement played when a meteor swarm begins.
    /// </summary>
    [DataField]
    public LocId? Announcement = "station-event-meteor-swarm-start-announcement";

    [DataField]
    public SoundSpecifier? AnnouncementSound = new SoundPathSpecifier("/Audio/Announcements/meteors.ogg")
    {
        Params = new()
        {
            Volume = -4
        }
    };

    /// <summary>
    /// Each meteor entity prototype and their corresponding weight in being picked.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, float> Meteors = new();

    [DataField]
    public MinMax Waves = new(3, 3);

    [DataField]
    public MinMax MeteorsPerWave = new(3, 4);

    [DataField]
    public MinMax WaveCooldown = new (10, 60);
}
