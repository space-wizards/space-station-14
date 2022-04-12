using System.Collections.Generic;
using Content.Shared.FixedPoint;
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
    public sealed class DamageVisualizerDataComponent : Component
    {
        public List<object> TargetLayerMapKeys = new();
        public bool Disabled = false;
        public bool Valid = true;
        public FixedPoint2 LastDamageThreshold = FixedPoint2.Zero;
        public Dictionary<object, bool> DisabledLayers = new();
        public Dictionary<object, string> LayerMapKeyStates = new();
        public Dictionary<string, FixedPoint2> LastThresholdPerGroup = new();
        public string TopMostLayerKey = default!;
    }
}
