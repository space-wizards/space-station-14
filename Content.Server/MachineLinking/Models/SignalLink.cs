using Content.Server.MachineLinking.Components;

namespace Content.Server.MachineLinking.Models
{
    public sealed class SignalLink
    {
        public readonly SignalTransmitterComponent TransmitterComponent;
        public readonly SignalReceiverComponent ReceiverComponent;
        public readonly SignalPort Transmitterport;
        public readonly SignalPort Receiverport;

        public SignalLink(SignalTransmitterComponent transmitterComponent, string transmitterPort, SignalReceiverComponent receiverComponent, string receiverPort)
        {
            TransmitterComponent = transmitterComponent;
            ReceiverComponent = receiverComponent;
            Transmitterport = TransmitterComponent.Outputs.GetPort(transmitterPort);
            Receiverport = ReceiverComponent.Inputs.GetPort(receiverPort);
        }
    }
}
