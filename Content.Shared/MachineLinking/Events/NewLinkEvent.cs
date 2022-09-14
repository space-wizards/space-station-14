namespace Content.Shared.MachineLinking.Events
{
    public sealed class NewLinkEvent : EntityEventArgs
    {
        public readonly EntityUid Transmitter;
        public readonly EntityUid Receiver;
        public readonly EntityUid? User;
        public readonly string TransmitterPort;
        public readonly string ReceiverPort;

        public NewLinkEvent(EntityUid? user, EntityUid transmitter, string transmitterPort, EntityUid receiver, string receiverPort)
        {
            User = user;
            Transmitter = transmitter;
            TransmitterPort = transmitterPort;
            Receiver = receiver;
            ReceiverPort = receiverPort;
        }
    }
}
