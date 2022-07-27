using Content.Shared.Radio;

namespace Content.Server.Radio.Components
{
    public interface IRadio : IComponent
    {
        void Receive(string message, RadioChannelPrototype channel, EntityUid speaker, bool announcement = false);

        void Broadcast(string message, EntityUid speaker, RadioChannelPrototype channel);
    }
}
