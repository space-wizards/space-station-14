using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSteeringSystem
{
    // TODO

    // Derived from RVO2 library which uses ORCA (optimal reciprocal collision avoidance).
    // Could also potentially use something force based or RVO or detour crowd.

    public bool CollisionAvoidanceEnabled { get; set; } = true;

    public bool ObstacleAvoidanceEnabled { get; set; } = true;

    private const float Radius = 0.35f;
    private const float RVO_EPSILON = 0.00001f;

    private void InitializeAvoidance()
    {
        var configManager = IoCManager.Resolve<IConfigurationManager>();
        configManager.OnValueChanged(CCVars.NPCCollisionAvoidance, SetCollisionAvoidance);
    }

    private void ShutdownAvoidance()
    {
        var configManager = IoCManager.Resolve<IConfigurationManager>();
        configManager.UnsubValueChanged(CCVars.NPCCollisionAvoidance, SetCollisionAvoidance);
    }

    // I deleted all of my relevant code for now as I only had dynamic body avoidance working and not static
    // but it will be added back real soon.
    private void SetCollisionAvoidance(bool obj)
    {
        CollisionAvoidanceEnabled = obj;
    }
}
