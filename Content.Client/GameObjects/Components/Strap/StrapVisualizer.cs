using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Strap;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.GameObjects.Components.Strap
{
    /// <summary>
    /// This manages an object with a <see cref="StrapComponent"/> visuals.
    /// You specify a buckledSuffix in the prototype, and the visualizer will look for
    /// [state]_buckledSuffix state in the object's sprite.
    ///
    /// By default this suffix is _buckled.
    /// </summary>
    [UsedImplicitly]
    public class StrapVisualizer : AppearanceVisualizer
    {
        [DataField("buckledSuffix", required: false)]
        private string? _buckledSuffix = "_buckled";

        // This array keeps the original layers and states of the sprite
        private ISpriteLayer[]? defaultStates;

        private ISpriteComponent? sprite;

        public virtual void InitializeEntity(IEntity entity)
        {

        }

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
                defaultStates = new ISpriteLayer[sprite.AllLayers.Count()];
                int i = 0;
                foreach (var layer in sprite.AllLayers)
                {
                    defaultStates[i] = layer;
                    i++;
                }
            }

            // Set all the layers state to [state]_buckled if it exists, or back to the default state
            if (appearance.TryGetData(StrapVisuals.StrapState, out bool buckled))
            {
                foreach (var layer in defaultStates)
                {
                    var newState = layer.RsiState + (buckled ? _buckledSuffix : "");

                    // If the icon state exists, assign it
                    var stateExists = sprite.BaseRSI?.TryGetState(newState, out var actualState);
                    if (stateExists != null && stateExists.Value)
                    {
                        sprite.LayerSetState(layer, newState);
                    }
                }
            }

        }
    }
}
