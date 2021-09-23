using Content.Server.Radio.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.Components
{
    /// <summary>
    ///     Marks a radio broadcaster, making any listened-in chat messages
    ///     broadcast over the radio.
    /// </summary>
    [RegisterComponent, Friend(typeof(RadioBroadcasterSystem))]
    public class RadioBroadcastOnListenComponent : Component
    {
        public override string Name => "RadioBroadcastOnListen";
    }
}
