using Robust.Shared.Prototypes;

namespace Content.Server.CallERT;

[Prototype("ertGroups")]
public sealed class ErtGroupsPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("groups")] public Dictionary<string, ErtGroupDetail> ErtGroupList = new();
}


[DataDefinition]
public sealed class ErtGroupDetail
{
    [DataField("name")] public string Name { get; } = string.Empty;
    [DataField("announcementAfterCall")] public string AnnouncementCall { get; } = string.Empty;

    [DataField("shuttle")]
    public string ShuttlePath = "Maps/Shuttles/med_ert_shuttle.yml";

    [DataField("humansList")]
    public Dictionary<string, int> HumansList = new ();

    [DataField("timeToSpawn")]
    public float WaitingTime = 600;

    [DataField("requirements")]
    public Dictionary<string, int> Requirements = new ();

    [DataField("shuttleTime")] public TimeSpan ShuttleTime { get; } = TimeSpan.FromMinutes(10);
}

