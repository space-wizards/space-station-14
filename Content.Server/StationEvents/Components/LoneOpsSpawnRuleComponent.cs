namespace Content.Server.StationEvents.Components;

[RegisterComponent]
public sealed class LoneOpsSpawnRuleComponent : Component
{
    public string LoneOpsShuttlePath = "Maps/Shuttles/striker.yml";

    public string GameRuleProto = "Nukeops";
}
