using System;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.Construction;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.EntitySystems;

public sealed class GasThermoSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasThermoMachineComponent, RefreshPartsEvent>(OnGasThermoRefreshParts);
    }

    private static void OnGasThermoRefreshParts(EntityUid uid, GasThermoMachineComponent component, RefreshPartsEvent args)
    {
        var matterBinRating = 0;
        var laserRating = 0;

        foreach (var part in args.Parts)
        {
            switch (part.PartType)
            {
                case MachinePart.MatterBin:
                    matterBinRating += part.Rating;
                    break;
                case MachinePart.Laser:
                    laserRating += part.Rating;
                    break;
            }
        }

        component.HeatCapacity = 5000 * MathF.Pow((matterBinRating - 1), 2);

        switch (component.Mode)
        {
            // 573.15K with stock parts.
            case ThermoMachineMode.Heater:
                component.MaxTemperature = Atmospherics.T20C + (component.InitialMaxTemperature * laserRating);
                break;
            // 73.15K with stock parts.
            case ThermoMachineMode.Freezer:
                component.MinTemperature = MathF.Max(Atmospherics.T0C - component.InitialMinTemperature + laserRating * 15f, Atmospherics.TCMB);
                break;
        }
    }
}
