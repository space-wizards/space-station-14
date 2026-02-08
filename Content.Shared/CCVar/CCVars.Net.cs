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

    public static readonly CVarDef<int> MaxNetBufferSizeConfigured =
        CVarDef.Create("net.max_net_buffer_size_configured", 8, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MinNetPredictTickBiasConfigured =
        CVarDef.Create("net.min_net_predict_bias_configured", 0, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxNetPredictTickBiasConfigured =
        CVarDef.Create("net.max_net_predict_bias_configured", 6, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MinNetPVSEntityBudgetConfigured =
        CVarDef.Create("net.min_net_pvs_budget_configured", 20, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxNetPVSEntityBudgetConfigured =
        CVarDef.Create("net.max_net_pvs_budget_configured", 150, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MinNetPVSEntityEnterBudgetConfigured =
        CVarDef.Create("net.min_net_pvs_entity_enter_budget_configured", 20, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxNetPVSEntityEnterBudgetConfigured =
        CVarDef.Create("net.max_net_pvs_entity_enter_budget_configured", 500, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MinNetPVSEntityExitBudgetConfigured =
        CVarDef.Create("net.min_net_pvs_entity_exit_budget_configured", 20, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxNetPVSEntityExitBudgetConfigured =
        CVarDef.Create("net.max_net_pvs_entity_exit_budget_configured", 300, CVar.CLIENTONLY | CVar.ARCHIVE);
}
