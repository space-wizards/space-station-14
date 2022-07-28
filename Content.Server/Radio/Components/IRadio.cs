using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;

namespace Content.Server.Radio.Components
{
    public interface IRadio : IComponent
    {
        void Broadcast(MessagePacket message);
    }
}
