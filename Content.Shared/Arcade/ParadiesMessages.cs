using Robust.Shared.Serialization;

namespace Content.Shared.Arcade;

public static class ParadiesMessages
{

    [Serializable, NetSerializable]
    public sealed class ParadiseArcadeConnectButtonPressedEvent : BoundUserInterfaceMessage
    {
    }

    /// <summary>
    /// Tells the client to connect to the specified destination.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ParadiseArcadeConnectEvent : BoundUserInterfaceMessage
    {
        public string Destination { get; }

        public ParadiseArcadeConnectEvent(string destination)
        {
            Destination = destination;
        }
    }
}
