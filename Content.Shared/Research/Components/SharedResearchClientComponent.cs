using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components
{
    /// <summary>
    ///     Sent to the server when the client deselects a research server.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ResearchClientServerDeselectedMessage : BoundUserInterfaceMessage
    {
        public ResearchClientServerDeselectedMessage()
        {
        }
    }

    /// <summary>
    ///     Sent to the server when the client chooses a research server.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ResearchClientServerSelectedMessage : BoundUserInterfaceMessage
    {
        public int ServerId;

        public ResearchClientServerSelectedMessage(int serverId)
        {
            ServerId = serverId;
        }
    }

    /// <summary>
    ///     Request that the server updates the client.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ResearchClientSyncMessage : BoundUserInterfaceMessage
    {

        public ResearchClientSyncMessage()
        {
        }
    }

    [NetSerializable, Serializable]
    public enum ResearchClientUiKey
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ResearchClientBoundInterfaceState : BoundUserInterfaceState
    {
        public int ServerCount;
        public string[] ServerNames;
        public int[] ServerIds;
        public int SelectedServerId;

        public ResearchClientBoundInterfaceState(int serverCount, string[] serverNames, int[] serverIds, int selectedServerId = -1)
        {
            ServerCount = serverCount;
            ServerNames = serverNames;
            ServerIds = serverIds;
            SelectedServerId = selectedServerId;
        }
    }
}
