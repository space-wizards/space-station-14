using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.FixedPoint;
using Robust.Shared.Physics;

namespace Content.Server.Starlight.Energy.Supermatter;
internal static class Const
{
    public static FixedPoint2 HeatPercent = 0.82f;
    public static FixedPoint2 BreakPercent = 0.04f;
    public static FixedPoint2 LightingPercent = 0.03f;
    public static FixedPoint2 RadiationPercent = 0.11f;

    public static GasProperties[] GasProperties =
    [
        new (0.24f), // oxygen
        new (0.20f), // nitrogen
        new (0.12f), // carbon dioxide
        new (0.60f), // plasma
        new (0.30f), // tritium
        new (0.14f), // vapor
        new (0.16f), // ommonium
        new (0.13f), // n2o
        new (1.00f), // frezon
    ];  
}
public record struct GasProperties(float HeatTransferPerMole);
