
using Content.Shared.Power;
using System.Numerics;

namespace Content.Client.Power;

internal sealed class PowerMonitoringHelper
{
    public static string CircleIconPath = "/Textures/Interface/PowerMonitoring/beveled_circle.png";
    public static string TriangleIconPath = "/Textures/Interface/PowerMonitoring/beveled_triangle.png";
    public static string SquareIconPath = "/Textures/Interface/PowerMonitoring/beveled_square.png";
    public static string HexagonIconPath = "/Textures/Interface/PowerMonitoring/beveled_hexagon.png";
    public static string SourceIconPath = "/Textures/Interface/PowerMonitoring/source_arrow.png";
    public static string LoadIconPath = "/Textures/Interface/PowerMonitoring/load_arrow.png";

    public static Color WallColor = new Color(102, 164, 217);
    public static Color TileColor = new Color(30, 57, 67);

    public static Dictionary<PowerMonitoringConsoleGroup, Color> PowerIconColors = new Dictionary<PowerMonitoringConsoleGroup, Color>
    {
        [PowerMonitoringConsoleGroup.Generator] = Color.Purple,
        [PowerMonitoringConsoleGroup.SMES] = Color.Orange,
        [PowerMonitoringConsoleGroup.Substation] = Color.Yellow,
        [PowerMonitoringConsoleGroup.APC] = Color.LimeGreen,
    };

    public static Dictionary<PowerMonitoringConsoleGroup, Color> DarkPowerIconColors = new Dictionary<PowerMonitoringConsoleGroup, Color>
    {
        [PowerMonitoringConsoleGroup.Generator] = new Color(54, 0, 54),
        [PowerMonitoringConsoleGroup.SMES] = new Color(82, 52, 0),
        [PowerMonitoringConsoleGroup.Substation] = new Color(80, 80, 0),
        [PowerMonitoringConsoleGroup.APC] = new Color(20, 76, 20),
    };


    public static Dictionary<CableType, Color> PowerCableColors = new Dictionary<CableType, Color>
    {
        [CableType.HighVoltage] = Color.Orange,
        [CableType.MediumVoltage] = Color.Yellow,
        [CableType.Apc] = Color.LimeGreen,
    };

    public static Dictionary<CableType, Color> DarkPowerCableColors = new Dictionary<CableType, Color>
    {
        [CableType.HighVoltage] = new Color(82, 52, 0),
        [CableType.MediumVoltage] = new Color(80, 80, 0),
        [CableType.Apc] = new Color(20, 76, 20),
    };

    public static Dictionary<CableType, Vector2> PowerCableOffsets = new Dictionary<CableType, Vector2>
    {
        [CableType.HighVoltage] = Vector2.Zero,
        [CableType.MediumVoltage] = new Vector2(-0.2f, -0.2f),
        [CableType.Apc] = new Vector2(0.2f, 0.2f),
    };
}
