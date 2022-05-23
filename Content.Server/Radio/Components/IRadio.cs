namespace Content.Server.Radio.Components
{
    public interface IRadio : IComponent
    {
        IReadOnlyList<int> Channels { get; }

        void Receive(string message, int channel, EntityUid speaker);

        void Broadcast(string message, EntityUid speaker);
    }
}
