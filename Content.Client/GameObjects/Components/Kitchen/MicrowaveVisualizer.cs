using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Kitchen;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public sealed class MicrowaveVisualizer : AppearanceVisualizer
    {    
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out MicrowaveVisualState state))
            {
                state = MicrowaveVisualState.Idle;
            }
            switch (state)
            {
                case MicrowaveVisualState.Idle:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_unlit");
                    break;

                case MicrowaveVisualState.Cooking:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_running_unlit");
                    break;

            }

            var glowingPartsVisible = !(component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && !powered);
            sprite.LayerSetVisible(MicrowaveVisualizerLayers.BaseUnlit, glowingPartsVisible);


        }

        public enum MicrowaveVisualizerLayers
        {
            Base,
            BaseUnlit
        }
    }


}
