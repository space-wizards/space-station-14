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

    public static FixedPoint2 DamageMultiplayer = 3.14f;

    public static GasProperties[] GasProperties =
    [
        new (0.24f, 1.20f, 1.4f), // oxygen
        new (0.20f, 1.46f, 1.5f), // nitrogen
        new (0.12f, 2.21f, 3.5f), // carbon dioxide
        new (0.60f, 0.61f, 1.3f), // plasma
        new (0.30f, 0.45f, 1.2f), // tritium
        new (0.14f, 2.31f, 2.5f), // vapor
        new (0.16f, 2.11f, 4.4f), // ommonium
        new (0.13f, 2.19f, 2.2f), // nitrous oxide
        new (1.00f, 0.01f, 1.1f), // frezon
    ];

    public static float MinPressure = 33f;
    public static float MaxPressure = 303.9f;

    public static float MaxTemperature = Atmospherics.T0C + 150;

    public static float EvaporationCompensation = 10;

    public static FixedPoint2 MaxDamagePerSecond = (100f / 120f) + RegenerationPerSecond; // Ensures it takes at least 2 minutes to deplete
    public static FixedPoint2 RegenerationPerSecond = 0.3f;

    public static string[] AudioCrack = ["/Audio/_Starlight/Effects/supermatter/crystal_crack_1.ogg", "/Audio/_Starlight/Effects/supermatter/crystal_crack_2.ogg"];
    public static string[] AudioBurn = ["/Audio/_Starlight/Effects/supermatter/burning_1.ogg", "/Audio/_Starlight/Effects/supermatter/burning_2.ogg", "/Audio/_Starlight/Effects/supermatter/burning_3.ogg"];
    public static string AudioEvaporate = "/Audio/_Starlight/Effects/supermatter/emitter2.ogg";
}
public record struct GasProperties(float HeatTransferPerMole, float HeatModifier, float RadiationStability);
