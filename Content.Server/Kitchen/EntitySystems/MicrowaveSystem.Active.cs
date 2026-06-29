using Content.Server.Construction;
using Content.Shared.Kitchen.Components;

namespace Content.Server.Kitchen.EntitySystems;

public sealed partial class MicrowaveSystem
{
    /// <summary>
    ///     Prevents construction graph operations as a result of temperature changes.
    /// </summary>
    /// <remarks>
    ///     For example: raw meat will not turn into steak while it is actively being microwaved.
    /// </remarks>
    /// <param name="ent">An entity that is actively being microwaved.</param>
    private void OnConstructionTemp(Entity<ActivelyMicrowavedComponent> ent, ref OnConstructionTemperatureEvent args)
    {
        args.Result = HandleResult.False;
    }
}
