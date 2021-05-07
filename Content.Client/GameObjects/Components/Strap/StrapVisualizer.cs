using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Strap;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Strap
{
    /// <summary>
    /// This manages an object with a <see cref="StrapComponent"/> visuals.
    /// You specify a buckledSuffix in the prototype, and the visualizer will look for
    /// [state]_buckledSuffix state for every layer in the sprite.
    /// If it doesn't find it, it leaves the state as-is.
    ///
    /// By default this suffix is _buckled.
    /// </summary>
    [UsedImplicitly]
    public class StrapVisualizer : AppearanceVisualizer
    {
        /// <summary>
        /// The suffix used to define the buckled states names in RSI file.
        /// [state] becomes [state][suffix],
        /// default suffix is _buckled, so chair becomes chair_buckled
        /// </summary>
        [ViewVariables][DataField("buckledSuffix", required: false)]
        private string? _buckledSuffix = "_buckled";

        private bool _isBuckled = false;


        public override void OnChangeData(AppearanceComponent appearance)
        {
            base.OnChangeData(appearance);

            if (!appearance.Owner.TryGetComponent(out SpriteComponent? sprite)) return;

            // Get appearance data, and check if the buckled state has changed
            if (!appearance.TryGetData(StrapVisuals.BuckledState, out bool buckled)) return;
            if (_isBuckled == buckled) return;

            _isBuckled = buckled;


            // Set all the layers state to [state]_buckled if it exists, or back to the default [state]

            var i = 0;
            foreach (var layer in sprite.AllLayers)
            {
                var newState = buckled
                    ? sprite.LayerGetState(i) + _buckledSuffix
                    : sprite.LayerGetState(i).ToString()?.Replace(_buckledSuffix ?? "", "");

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
