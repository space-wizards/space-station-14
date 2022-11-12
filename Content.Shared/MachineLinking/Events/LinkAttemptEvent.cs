namespace Content.Shared.MachineLinking.Events
{
    public sealed class LinkAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid Transmitter;
        public readonly EntityUid Receiver;
        public readonly EntityUid? User;
        public readonly string TransmitterPort;
        public readonly string ReceiverPort;

        public LinkAttemptEvent(EntityUid? user, EntityUid transmitter, string transmitterPort, EntityUid receiver, string receiverPort)
        {
            User = user;
            Transmitter = transmitter;
            TransmitterPort = transmitterPort;
            Receiver = receiver;
            ReceiverPort = receiverPort;
        }
    }
}
