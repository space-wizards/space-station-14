using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MeteorSwarmRule))]
public sealed class MeteorSwarmRuleComponent : Component
{
    public float _cooldown;

    /// <summary>
    /// We'll send a specific amount of waves of meteors towards the station per ending rather than using a timer.
    /// </summary>
    public int _waveCounter;

    public int MinimumWaves = 3;
    public int MaximumWaves = 8;

    public float MinimumCooldown = 10f;
    public float MaximumCooldown = 60f;

    public int MeteorsPerWave = 5;
    public float MeteorVelocity = 10f;
    public float MaxAngularVelocity = 0.25f;
    public float MinAngularVelocity = -0.25f;
}
