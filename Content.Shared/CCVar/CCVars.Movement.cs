using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Is mob pushing enabled.
    /// </summary>
    public static readonly CVarDef<bool> MovementMobPushing =
        CVarDef.Create("movement.mob_pushing", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Can we push mobs not moving.
    /// </summary>
    public static readonly CVarDef<bool> MovementPushingStatic =
        CVarDef.Create("movement.pushing_static", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Dot product for the pushed entity's velocity to a target entity's velocity before it gets moved.
    /// </summary>
    public static readonly CVarDef<float> MovementPushingVelocityProduct =
        CVarDef.Create("movement.pushing_velocity_product", 0.0f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Cap for how much an entity can be pushed per second.
    /// </summary>
    public static readonly CVarDef<float> MovementPushingCap =
        CVarDef.Create("movement.pushing_cap", 2f, CVar.SERVER | CVar.REPLICATED);
}
