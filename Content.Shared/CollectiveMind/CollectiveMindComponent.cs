using Robust.Shared.GameStates;

namespace Content.Shared.CollectiveMind
{
    [RegisterComponent, NetworkedComponent]
    public sealed class CollectiveMindComponent : Component
    {
        [DataField("channel", required: true)]
        public string Channel = string.Empty;

        [DataField("channelColor")]
        public Color ChannelColor = Color.Lime;
    }
}
