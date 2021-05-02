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

        // This array keeps the original layers' states of the sprite
        private string?[]? defaultStates;

        // This keeps a reference to the entity's SpriteComponent to avoid fetching it each time
        private ISpriteComponent? sprite;


        public override void OnChangeData(AppearanceComponent appearance)
        {
            base.OnChangeData(appearance);

            // Do nothing if the object doesn't have a SpriteComponent
            if (sprite == null)
                if (appearance.Owner.TryGetComponent(out ISpriteComponent? s))
                {
                    this.sprite = s;    // Keep a reference to the sprite for good measure
                } else return;


            // If defaultStates is empty, initialize it
            if (defaultStates == null)
            {
                // Create
                var count = sprite.AllLayers.Count();
                defaultStates = new string[count];

                // Fill
                var i = 0;
                foreach (var layer in sprite.AllLayers)
                {
                    defaultStates[i] = layer.RsiState.Name;
                    i++;
                }
            }

            // Set all the layers state to [state]_buckled if it exists, or back to the default [state]
            if (appearance.TryGetData(StrapVisuals.BuckledState, out bool buckled))
            {
                // Otherwise, change each layer's state
                var i = 0;
                foreach (var state in defaultStates)
                {
                    var newState = state + (buckled ? _buckledSuffix : "");

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
}
