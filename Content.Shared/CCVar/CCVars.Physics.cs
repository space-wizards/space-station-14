using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     When a mob is walking should its X / Y movement be relative to its parent (true) or the map (false).
    /// </summary>
    public static readonly CVarDef<bool> RelativeMovement =
        CVarDef.Create("physics.relative_movement", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> TileFrictionModifier =
        CVarDef.Create("physics.tile_friction", 40.0f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> StopSpeed =
        CVarDef.Create("physics.stop_speed", 0.1f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Whether mobs can push objects like lockers.
    /// </summary>
    /// <remarks>
    ///     Technically client doesn't need to know about it but this may prevent a bug in the distant future so it stays.
    /// </remarks>
    public static readonly CVarDef<bool> MobPushing =
        CVarDef.Create("physics.mob_pushing", false, CVar.REPLICATED | CVar.SERVER);
}
