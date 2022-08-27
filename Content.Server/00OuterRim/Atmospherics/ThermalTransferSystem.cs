using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Robust.Shared.Map;

namespace Content.Server._00OuterRim.Atmospherics;

/// <summary>
/// This handles thermal devices, namely thermal pumps.
/// </summary>
public sealed class ThermalTransferSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (transmitter, apcReceiver, temperature, xform) in EntityQuery<ThermalTransmitterComponent, ApcPowerReceiverComponent, TemperatureComponent, TransformComponent>())
        {
            if (apcReceiver.Powered == false)
                continue; // No power, no conduct.

            var left = xform.LocalRotation.RotateVec(new Vector2(-1, 0)).Rounded().Floored();
            var right = xform.LocalRotation.RotateVec(new Vector2(1, 0)).Rounded().Floored();

            var leftTile = _atmosphereSystem.GetTileMixture(xform.GridUid, xform.MapUid,
                xform.Coordinates.Offset(left).ToVector2i(EntityManager, _mapManager));

            var rightTile = _atmosphereSystem.GetTileMixture(xform.GridUid, xform.MapUid,
                xform.Coordinates.Offset(right).ToVector2i(EntityManager, _mapManager));

            if (leftTile is not null && leftTile.Immutable == false)
            {
                var leftEv = new AtmosExposedUpdateEvent(xform.Coordinates.Offset(left), leftTile, xform);
                RaiseLocalEvent(transmitter.Owner, ref leftEv);
            }
            else
            {
                if (!(transmitter.EasyMode && temperature.CurrentTemperature >= Shared.Atmos.Atmospherics.T20C))
                {
                    _temperatureSystem.ChangeHeat(transmitter.Owner, -transmitter.Watts * frameTime, true);
                }
            }

            if (rightTile is not null && rightTile.Immutable == false)
            {
                var rightEv = new AtmosExposedUpdateEvent(xform.Coordinates.Offset(right), rightTile, xform);
                RaiseLocalEvent(transmitter.Owner, ref rightEv);
            }
            else
            {
                if (!(transmitter.EasyMode && temperature.CurrentTemperature >= Shared.Atmos.Atmospherics.T20C))
                {
                    _temperatureSystem.ChangeHeat(transmitter.Owner, -transmitter.Watts * frameTime, true);
                }
            }
        }
    }

    private void ShareWithTile(TemperatureComponent temp, GasMixture gasMixture)
    {

    }
}
