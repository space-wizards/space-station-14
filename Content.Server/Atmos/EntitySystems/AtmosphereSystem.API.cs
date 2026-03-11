using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /*
     General API for interacting with AtmosphereSystem.

     If you feel like you're stepping on eggshells because you can't access things in AtmosphereSystem,
     consider adding a method here instead of making your own way to work around it.

     The API is spread out across many partials for organization, see the partials
     for more specific APIs pertaining to the different Atmospherics subsystems.
     */

    /// <summary>
    /// Returns a speedup factor for pumped or flow-based devices that depend on MaxTransferRate.
    /// </summary>
    /// <remarks>Pulls the speedup CVAR.
    /// Devices that depend on or are clamped by <see cref="Atmospherics.MaxTransferRate"/>
    /// should always multiply their transfer rate clamp by this value.</remarks>
    [PublicAPI]
    public float PumpSpeedup()
    {
        // (in counterstrike counter-terrorist voice) EASY PEASY LEMON SQUEEZY
        return Speedup;
    }
}
