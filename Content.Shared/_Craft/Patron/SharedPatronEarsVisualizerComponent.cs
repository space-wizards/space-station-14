using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Patron
{
    [NetworkedComponent]
    [RegisterComponent]
    [Access(typeof(SharedPatronSystem))]
    public sealed class PatronEarsVisualizerComponent : Component
    {
        [DataField("sprite")]
        public string RsiPath = "Clothing/Head/Hats/catears.rsi";
    }

    [Serializable, NetSerializable]
    public sealed class PatronEarsVisualizerComponentState : ComponentState
    {
        public string RsiPath { get; init; }
        public PatronEarsVisualizerComponentState(string rsiPath)
        {
            RsiPath = rsiPath;
        }
    }
}
