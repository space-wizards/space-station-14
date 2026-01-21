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

    public static readonly CVarDef<int> GasOverlayTempResolution =
        CVarDef.Create("net.gasoverlaytemperatureresolution",
            250,
            CVar.SERVER | CVar.REPLICATED,
            "Resolution of tempearture data send to the client. If for example set to 10, client will get info about temp on the scale from 0 to 9 (maximum 255)");

    public static readonly CVarDef<int> GasOverlayTempMinimum =
        CVarDef.Create("net.gasoverlaytempminimum",
            0,
            CVar.SERVER | CVar.REPLICATED,
            "Minimal temperature data send to the client");

    public static readonly CVarDef<int> GasOverlayTempMaximum =
        CVarDef.Create("net.gasoverlaytempmaximum",
            1000,
            CVar.SERVER | CVar.REPLICATED,
            "Maximum tempearture data send to the client.");
}
