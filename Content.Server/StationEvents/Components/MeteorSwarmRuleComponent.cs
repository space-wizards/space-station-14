using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MeteorSwarmRule))]
public sealed class MeteorSwarmRuleComponent : Component
{
    [DataField("cooldown")]
    public float Cooldown;

    /// <summary>
    /// We'll send a specific amount of waves of meteors towards the station per ending rather than using a timer.
    /// </summary>
    [DataField("waveCounter")]
    public int WaveCounter;

    [DataField("minimumWaves")]
    public int MinimumWaves = 3;

    [DataField("maximumWaves")]
    public int MaximumWaves = 8;

    [DataField("minimumCooldown")]
    public float MinimumCooldown = 10f;

    [DataField("maximumCooldown")]
    public float MaximumCooldown = 60f;

    [DataField("meteorsPerWave")]
    public int MeteorsPerWave = 5;

    [DataField("meteorVelocity")]
    public float MeteorVelocity = 10f;

    [DataField("maxAngularVelocity")]
    public float MaxAngularVelocity = 0.25f;

    [DataField("minAngularVelocity")]
    public float MinAngularVelocity = -0.25f;
}
