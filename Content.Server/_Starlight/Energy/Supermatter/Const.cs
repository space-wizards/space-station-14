using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Atmos;
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
        new (0.24f, 0.90f), // oxygen
        new (0.20f, 0.64f), // nitrogen
        new (0.12f, 1.11f), // carbon dioxide
        new (0.60f, 0.21f), // plasma
        new (0.30f, 0.45f), // tritium
        new (0.14f, 1.21f), // vapor
        new (0.16f, 0.91f), // ommonium
        new (0.13f, 0.99f), // n2o
        new (1.00f, 0.01f), // frezon
    ];

    public static float MinPressure = 33f;
    public static float MaxPressure = 303.9f;

    public static float MaxTemperature = Atmospherics.T0C + 150;

    public static string[] AudioCrack = ["/Audio/_Starlight/Effects/supermatter/crystal_crack_1.ogg", "/Audio/_Starlight/Effects/supermatter/crystal_crack_2.ogg"];
    public static string[] AudioBurn = ["/Audio/_Starlight/Effects/supermatter/burning_1.ogg", "/Audio/_Starlight/Effects/supermatter/burning_2.ogg", "/Audio/_Starlight/Effects/supermatter/burning_3.ogg"];
}
public record struct GasProperties(float HeatTransferPerMole, float HeatModifier);
