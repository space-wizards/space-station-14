using Content.Server.Radio.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.Components
{
    /// <summary>
    ///     Marks a radio receiver, making all received messages relay
    ///     to whoever is wearing it
    /// </summary>
    [RegisterComponent, Friend(typeof(RadioListenerSystem))]
    public class RadioRelayToWearerComponent : Component
    {
        public override string Name => "RadioRelayToWearer";
    }
}
