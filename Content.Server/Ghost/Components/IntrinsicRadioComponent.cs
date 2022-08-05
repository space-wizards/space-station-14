using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;

namespace Content.Server.Ghost.Components
{
    /// <summary>
    /// Add to a particular entity to let it receive messages from the specified channels.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    public sealed class IntrinsicRadioComponent : Component, IRadio
    {
        public void Broadcast(MessagePacket message)
        {
        }
    }
}
