using Robust.Shared.Configuration;

namespace Content.Shared.NullLink.CCVar;
[CVarDefs]
public sealed partial class NullLinkCCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("nulllink.enabled", false, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> ClusterConnectionString =
        CVarDef.Create("nulllink.cluster_connection_string", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> Project =
        CVarDef.Create("nulllink.id.project", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> Server =
        CVarDef.Create("nulllink.id.server", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<ServerType> Type =
        CVarDef.Create("nulllink.hub.server_type", ServerType.NRP, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> Title =
        CVarDef.Create("nulllink.hub.title", "MyServer", CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<string> Description =
        CVarDef.Create("nulllink.hub.description", "----", CVar.SERVERONLY);

    public static readonly CVarDef<bool> IsAdultOnly =
        CVarDef.Create("nulllink.is_adult_only", false, CVar.REPLICATED | CVar.SERVER);
}

public enum ServerType
{
    NRP,
    LRP_minus,
    LRP,
    LRP_plus,
    MRP_minus,
    MRP,
    MRP_plus,
    HRP_minus,
    HRP,
    HRP_plus
}
