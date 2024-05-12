namespace Content.Server.Radio.EntitySystems;

public sealed class MessagesStorageSystem : EntitySystem
{
    private Dictionary<StationId,Dictionary<uint,EntityUid>> EntityDict;

    public MessagesStorageComponent? GetStorage(StationId stationId, uint serverFrequency)
    {
        if (EntityDict.TryGetValue(stationId, out var deeperDict))
        {
            if (deeperDict.TryGetValue(serverFrequency, out var targetUid))
                return CompOrNull<MessagesStorageComponent>(targetUid);
            var newServer = CreateStorage(stationId, serverFrequency)
            deeperDict[serverFrequency] = newServer;
            return newServer;
        }

        var newServer = CreateStorage(stationId, serverFrequency)
        EntityDict[stationId] = new Dictionary<uint, EntityUid>();
        EntityDict[stationId][serverFrequency] = newServer;
        return newServer;
    }
}
