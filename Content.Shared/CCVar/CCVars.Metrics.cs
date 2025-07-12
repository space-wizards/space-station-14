namespace Content.Shared.CCVar;

using Robust.Shared.Configuration;

public sealed partial class CCVars
{
    /// <summary>
    ///     Controls if admin logs are enabled. Highly recommended to shut this off for development.
    /// </summary>
    public static readonly CVarDef<bool> BasicMetricLoggingEnabled =
        CVarDef.Create("basicMetrics.logging_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<string> BasicMetricsServerName =
        CVarDef.Create("basicMetrics.server_name", "", CVar.SERVERONLY);
}
