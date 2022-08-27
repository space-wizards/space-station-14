using Content.Server.Administration;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server._00OuterRim.Atmospherics;

/// <summary>
/// This handles...
/// </summary>
public sealed class PassiveHeaterSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ApcPassiveHeaterComponent, ComponentStartup>(OnApcPassiveHeaterStartup);
        _consoleHost.RegisterCommand("calcheatprod", "Calculates how much heat energy a vessel will produce.", "calcheatprod <grid eid>", CalcHeatProd);
    }

    [AdminCommand(AdminFlags.Debug)]
    private void CalcHeatProd(IConsoleShell shell, string argstr, string[] args)
    {

    }

    private void OnApcPassiveHeaterStartup(EntityUid uid, ApcPassiveHeaterComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out TemperatureComponent? temp))
            return;

        var xform = Transform(uid);

        var tile = _atmosphereSystem.GetTileMixture(xform.GridUid, xform.MapUid,
            xform.Coordinates.ToVector2i(EntityManager, _mapManager));

        if (tile is null || tile.Immutable)
        {
            temp.CurrentTemperature = Shared.Atmos.Atmospherics.TCMB;
            return;
        }

        temp.CurrentTemperature = tile.Temperature;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (heater, apcReceiver, temperature, xform) in EntityQuery<ApcPassiveHeaterComponent, ApcPowerReceiverComponent, TemperatureComponent, TransformComponent>())
        {
            // Author's note: I initially forgot the frametime factor and baked myself alive.
            _temperatureSystem.ChangeHeat(temperature.Owner, apcReceiver.Load * frameTime, true, temperature);
        }
    }
}
