using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     When a mob is walking should its X / Y movement be relative to its parent (true) or the map (false).
    /// </summary>
    public static readonly CVarDef<bool> RelativeMovement =
        CVarDef.Create("physics.relative_movement", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> MinFriction =
        CVarDef.Create("physics.min_friction", 0.0f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> AirFriction =
        CVarDef.Create("physics.air_friction", 0.2f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> OffgridFriction =
        CVarDef.Create("physics.offgrid_friction", 0.05f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> TileFrictionModifier =
        CVarDef.Create("physics.tile_friction", 8.0f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> StopSpeed =
        CVarDef.Create("physics.stop_speed", 0.1f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);
}
