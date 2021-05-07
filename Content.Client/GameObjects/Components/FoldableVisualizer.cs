using System.Linq;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Strap;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components
{

    /// <summary>
    /// Updates the visual state for <see cref="SharedFoldableComponent"/>
    /// </summary>
    [UsedImplicitly]
    public class FoldableVisualizer : AppearanceVisualizer
    {
        /// <summary>
        /// The suffix used to define the buckled states names in RSI file.
        /// [state] becomes [state][suffix],
        /// default suffix is _buckled, so chair becomes chair_buckled
        /// </summary>
        [ViewVariables][DataField("foldedSuffix", required: false)]
        private string? _foldedSuffix = "_folded";

        // This keeps a reference to the entity's SpriteComponent to avoid fetching it each time
        private ISpriteComponent? _sprite;

        private bool? _isFolded;

        public override void OnChangeData(AppearanceComponent appearance)
        {
            base.OnChangeData(appearance);

            if (!appearance.Owner.TryGetComponent(out SpriteComponent? sprite)) return;

            // Get appearance data, and check if the folded state has changed
            if (!appearance.TryGetData(FoldableVisuals.FoldedState, out bool folded)) return;

            _isFolded = folded;


            // Set all the layers state to [state]_folded if it exists, or back to the default [state]

            var i = 0;
            foreach (var layer in sprite.AllLayers)
            {
                var newState = folded
                    ? sprite.LayerGetState(i) + _foldedSuffix
                    : sprite.LayerGetState(i).ToString()?.Replace(_foldedSuffix ?? "", "");

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
