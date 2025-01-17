using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Whether gas differences will move entities.
    /// </summary>
    public static readonly CVarDef<bool> SpaceWind =
        CVarDef.Create("atmos.space_wind", false, CVar.SERVERONLY);

    /// <summary>
    ///     Divisor from maxForce (pressureDifference * 2.25f) to force applied on objects.
    /// </summary>
    public static readonly CVarDef<float> SpaceWindPressureForceDivisorThrow =
        CVarDef.Create("atmos.space_wind_pressure_force_divisor_throw", 15f, CVar.SERVERONLY);

    /// <summary>
    ///     Divisor from maxForce (pressureDifference * 2.25f) to force applied on objects.
    /// </summary>
    public static readonly CVarDef<float> SpaceWindPressureForceDivisorPush =
        CVarDef.Create("atmos.space_wind_pressure_force_divisor_push", 2500f, CVar.SERVERONLY);

    /// <summary>
    ///     The maximum velocity (not force) that may be applied to an object by atmospheric pressure differences.
    ///     Useful to prevent clipping through objects.
    /// </summary>
    public static readonly CVarDef<float> SpaceWindMaxVelocity =
        CVarDef.Create("atmos.space_wind_max_velocity", 30f, CVar.SERVERONLY);

    /// <summary>
    ///     The maximum force that may be applied to an object by pushing (i.e. not throwing) atmospheric pressure differences.
    ///     A "throwing" atmospheric pressure difference ignores this limit, but not the max. velocity limit.
    /// </summary>
    public static readonly CVarDef<float> SpaceWindMaxPushForce =
        CVarDef.Create("atmos.space_wind_max_push_force", 20f, CVar.SERVERONLY);

    /// <summary>
    ///     Whether monstermos tile equalization is enabled.
    /// </summary>
    public static readonly CVarDef<bool> MonstermosEqualization =
        CVarDef.Create("atmos.monstermos_equalization", true, CVar.SERVERONLY);

    /// <summary>
    ///     Whether monstermos explosive depressurization is enabled.
    ///     Needs <see cref="MonstermosEqualization"/> to be enabled to work.
    /// </summary>
    public static readonly CVarDef<bool> MonstermosDepressurization =
        CVarDef.Create("atmos.monstermos_depressurization", true, CVar.SERVERONLY);

    /// <summary>
    ///     Whether monstermos explosive depressurization will rip tiles..
    ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
    ///     WARNING: This cvar causes MAJOR contrast issues, and usually tends to make any spaced scene look very cluttered.
    ///     This not only usually looks strange, but can also reduce playability for people with impaired vision. Please think twice before enabling this on your server.
    ///     Also looks weird on slow spacing for unrelated reasons. If you do want to enable this, you should probably turn on instaspacing.
    /// </summary>
    public static readonly CVarDef<bool> MonstermosRipTiles =
        CVarDef.Create("atmos.monstermos_rip_tiles", false, CVar.SERVERONLY);

    /// <summary>
    ///     Whether explosive depressurization will cause the grid to gain an impulse.
    ///     Needs <see cref="MonstermosEqualization"/> and <see cref="MonstermosDepressurization"/> to be enabled to work.
    /// </summary>
    public static readonly CVarDef<bool> AtmosGridImpulse =
        CVarDef.Create("atmos.grid_impulse", false, CVar.SERVERONLY);

    /// <summary>
    ///     What fraction of air from a spaced tile escapes every tick.
    ///     1.0 for instant spacing, 0.2 means 20% of remaining air lost each time
    /// </summary>
    public static readonly CVarDef<float> AtmosSpacingEscapeRatio =
        CVarDef.Create("atmos.mmos_spacing_speed", 0.15f, CVar.SERVERONLY);

    /// <summary>
    ///     Minimum amount of air allowed on a spaced tile before it is reset to 0 immediately in kPa
    ///     Since the decay due to SpacingEscapeRatio follows a curve, it would never reach 0.0 exactly
    ///     unless we truncate it somewhere.
    /// </summary>
    public static readonly CVarDef<float> AtmosSpacingMinGas =
        CVarDef.Create("atmos.mmos_min_gas", 2.0f, CVar.SERVERONLY);

    /// <summary>
    ///     How much wind can go through a single tile before that tile doesn't depressurize itself
    ///     (I.e spacing is limited in large rooms heading into smaller spaces)
    /// </summary>
    public static readonly CVarDef<float> AtmosSpacingMaxWind =
        CVarDef.Create("atmos.mmos_max_wind", 500f, CVar.SERVERONLY);

    /// <summary>
    ///     Whether atmos superconduction is enabled.
    /// </summary>
    /// <remarks> Disabled by default, superconduction is awful. </remarks>
    public static readonly CVarDef<bool> Superconduction =
        CVarDef.Create("atmos.superconduction", false, CVar.SERVERONLY);

    /// <summary>
    ///     Heat loss per tile due to radiation at 20 degC, in W.
    /// </summary>
    public static readonly CVarDef<float> SuperconductionTileLoss =
        CVarDef.Create("atmos.superconduction_tile_loss", 30f, CVar.SERVERONLY);

    /// <summary>
    ///     Whether excited groups will be processed and created.
    /// </summary>
    public static readonly CVarDef<bool> ExcitedGroups =
        CVarDef.Create("atmos.excited_groups", true, CVar.SERVERONLY);

    /// <summary>
    ///     Whether all tiles in an excited group will clear themselves once being exposed to space.
    ///     Similar to <see cref="MonstermosDepressurization"/>, without none of the tile ripping or
    ///     things being thrown around very violently.
    ///     Needs <see cref="ExcitedGroups"/> to be enabled to work.
    /// </summary>
    public static readonly CVarDef<bool> ExcitedGroupsSpaceIsAllConsuming =
        CVarDef.Create("atmos.excited_groups_space_is_all_consuming", false, CVar.SERVERONLY);

    /// <summary>
    ///     Maximum time in milliseconds that atmos can take processing.
    /// </summary>
    public static readonly CVarDef<float> AtmosMaxProcessTime =
        CVarDef.Create("atmos.max_process_time", 3f, CVar.SERVERONLY);

    /// <summary>
    ///     Atmos tickrate in TPS. Atmos processing will happen every 1/TPS seconds.
    /// </summary>
    public static readonly CVarDef<float> AtmosTickRate =
        CVarDef.Create("atmos.tickrate", 15f, CVar.SERVERONLY);

    /// <summary>
    ///     Scale factor for how fast things happen in our atmosphere
    ///     simulation compared to real life. 1x means pumps run at 1x
    ///     speed. Players typically expect things to happen faster
    ///     in-game.
    /// </summary>
    public static readonly CVarDef<float> AtmosSpeedup =
        CVarDef.Create("atmos.speedup", 8f, CVar.SERVERONLY);

    /// <summary>
    ///     Like atmos.speedup, but only for gas and reaction heat values. 64x means
    ///     gases heat up and cool down 64x faster than real life.
    /// </summary>
    public static readonly CVarDef<float> AtmosHeatScale =
        CVarDef.Create("atmos.heat_scale", 8f, CVar.SERVERONLY);

    /// <summary>
    ///     Maximum explosion radius for explosions caused by bursting a gas tank ("max caps").
    ///     Setting this to zero disables the explosion but still allows the tank to burst and leak.
    /// </summary>
    public static readonly CVarDef<float> AtmosTankFragment =
        CVarDef.Create("atmos.max_explosion_range", 26f, CVar.SERVERONLY);
}
