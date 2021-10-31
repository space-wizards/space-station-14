using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Client.Damage
{
    // Stores all the data for a DamageVisualizer.
    //
    // Storing it inside of the AppearanceComponent's data
    // dictionary was too messy, but at least we can
    // store it in the entity itself as a separate,
    // dynamically added component.
    [RegisterComponent]
    public class DamageVisualizerDataComponent : Component
    {
        public override string Name => "DamageVisualizerData";

        public List<object> TargetLayerMapKeys = new();
        public bool Disabled = false;
        public bool Valid = true;
        public int LastDamageThreshold = 0;
        public Dictionary<object, bool> DisabledLayers = new();
        public Dictionary<object, string> LayerMapKeyStates = new();
        public Dictionary<string, int> LastThresholdPerGroup = new();
        public string TopMostLayerKey = default!;
    }
}
