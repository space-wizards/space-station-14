using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Ghost.Components
{
    /// <summary>
    /// Add to a particular entity to let it receive messages from the specified channels.
    /// This class only exists so the radio system knows that you want to listen in directly
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    public sealed class GhostRadioComponent : Component, IRadio
    {
        public void Broadcast(MessagePacket message)
        {
        }
    }
}
