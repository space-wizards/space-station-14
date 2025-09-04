using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<float> NetAtmosDebugOverlayTickRate =
        CVarDef.Create("net.atmosdbgoverlaytickrate", 3.0f);

    public static readonly CVarDef<float> NetGasOverlayTickRate =
        CVarDef.Create("net.gasoverlaytickrate", 3.0f);

    public static readonly CVarDef<int> GasOverlayThresholds =
        CVarDef.Create("net.gasoverlaythresholds", 20);

    public static readonly CVarDef<float> GasOverlayHeatThreshold =
        CVarDef.Create("net.gasoverlayheatthreshold",
            0.05f,
            CVar.SERVER | CVar.REPLICATED,
            "Threshold for sending tile temperature updates to client in percent of distortion strength," +
            "from 0.0 to 1.0. Example: 0.05 = 5%, which means heat distortion will appear in 20 'steps'.");

    public static readonly CVarDef<float> GasOverlayHeatMinimum =
        CVarDef.Create("net.gasoverlayheatminimum",
            325f,
            CVar.SERVER | CVar.REPLICATED,
            "Temperature at which heat distortion effect will begin to apply.");

    public static readonly CVarDef<float> GasOverlayHeatMaximum =
        CVarDef.Create("net.gasoverlayheatmaximum",
            1000f,
            CVar.SERVER | CVar.REPLICATED,
            "Temperature at which heat distortion effect will be at maximum strength.");
}
