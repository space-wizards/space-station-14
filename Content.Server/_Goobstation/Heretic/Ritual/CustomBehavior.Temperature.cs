using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Heretic.Prototypes;

namespace Content.Server.Heretic.Ritual;

[Virtual] public sealed partial class RitualTemperatureBehavior : RitualCustomBehavior
{
    /// <summary>
    ///     Min temp in celsius
    /// </summary>
    [DataField] public float MinThreshold = 0f;

    /// <summary>
    ///     Max temp in celsius
    /// </summary>
    [DataField] public float MaxThreshold = float.PositiveInfinity;

    private AtmosphereSystem _atmos = default!;

    public override bool Execute(RitualData args, out string? outstr)
    {
        outstr = null;

        _atmos = args.EntityManager.System<AtmosphereSystem>();

        var mix = _atmos.GetTileMixture(args.Platform);

        if (mix == null || mix.TotalMoles == 0) // just accept space as it is
            return true;

        if (mix.Temperature > Atmospherics.T0C + MaxThreshold)
        {
            outstr = Loc.GetString("heretic-ritual-fail-temperature-hot");
            return false;
        }
        if (mix.Temperature > Atmospherics.T0C + MinThreshold)
        {
            outstr = Loc.GetString("heretic-ritual-fail-temperature-cold");
            return false;
        }

        return true;
    }

    public override void Finalize(RitualData args)
    {
        // do nothing
    }
}
