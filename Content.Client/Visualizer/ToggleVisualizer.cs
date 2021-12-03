using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.Visualizer
{
    /// <summary>
    /// General purpose appearance visualizer used to toggle back and forth between two states
    /// </summary>
    [UsedImplicitly]
    public class ToggleVisualizer : AppearanceVisualizer
    {
        /// <summary>
        /// The suffix used to name the state. Visualizer switches between [state] and [state]_[suffix]
        /// For example "object", "object_enabled"
        /// </summary>
        [DataField("stateSuffix", required: true)]
        private string? _suffix;

        /// <summary>
        /// The key the visualizer uses to get toggled state data
        /// </summary>
        [DataField("key", required: false)]
        private string _key = "Toggle";

        public override void OnChangeData(AppearanceComponent appearance)
        {
            base.OnChangeData(appearance);

            if (!appearance.Owner.TryGetComponent(out SpriteComponent? sprite)) return;

            if (!appearance.TryGetData(_key, out bool toggle)) return;

            // Set all the layers state to [state]_[suffix] if it exists, or back to the default [state]
            // If they don't exist, leave them like that
            var i = 0;

            foreach (var layer in sprite.AllLayers)
            {
                if (string.IsNullOrEmpty(_suffix))
                    continue;

                var newState = toggle
                    ? sprite.LayerGetState(i) + _suffix
                    : sprite.LayerGetState(i).ToString()?.Replace(_suffix, "");

                // Check if the state actually exists in the RSI and set it on the layer
                var stateExists = sprite.BaseRSI?.TryGetState(newState, out var actualState);
                if (stateExists ?? false)
                {
                    sprite.LayerSetState(i, newState);
                }

                i++;
            }
        }
    }
}
