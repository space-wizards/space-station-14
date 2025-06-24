namespace Content.Shared.CCVar;

using Robust.Shared.Configuration;

public sealed partial class CCVars
{
    /// <summary>
    ///     Controls if admin logs are enabled. Highly recommended to shut this off for development.
    /// </summary>
    public static readonly CVarDef<bool> MetricLoggingEnabled =
        CVarDef.Create("metrics.logging_enabled", true, CVar.SERVERONLY);

    public static readonly CVarDef<string> MetricsServerName =
        CVarDef.Create("metrics.server_name", "", CVar.SERVERONLY);
}
