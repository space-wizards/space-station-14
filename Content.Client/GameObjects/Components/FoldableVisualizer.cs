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

            // TODO : Stop caching components
            // Do nothing if the object doesn't have a SpriteComponent
            if (_sprite == null)
                if (appearance.Owner.TryGetComponent(out ISpriteComponent? s))
                {
                    this._sprite = s;    // Keep a reference to the sprite for good measure
                } else return;

            // Get appearance data, and check if the folded state has changed
            if (!appearance.TryGetData(FoldableVisuals.FoldedState, out bool folded)) return;

            // Set all the layers state to [state]_folded if it exists, or back to the default [state]

            _isFolded = folded;

            var i = 0;
            foreach (var layer in _sprite.AllLayers)
            {
                var newState = folded
                    ? _sprite.LayerGetState(i) + _foldedSuffix
                    : _sprite.LayerGetState(i).ToString()?.Replace(_foldedSuffix ?? "", "");

                // Check if the state actually exists in the RSI and set it on the layer
                var stateExists = _sprite.BaseRSI?.TryGetState(newState, out var actualState);
                if (stateExists ?? false)
                {
                    _sprite.LayerSetState(i, newState);
                }

                i++;
            }

        }
    }
}
