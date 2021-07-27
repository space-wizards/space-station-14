using Content.Client.Kitchen.Components;
using Content.Client.Kitchen.EntitySystems;
using Content.Shared.Kitchen.Components;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Client.Kitchen.Visualizers
{
    [UsedImplicitly]
    public sealed class MicrowaveVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();

            var microwaveSystem = EntitySystem.Get<MicrowaveSystem>();
            var microwaveComponent = component.Owner.GetComponentOrNull<MicrowaveComponent>();

            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out MicrowaveVisualState state))
            {
                state = MicrowaveVisualState.Idle;
            }
            switch (state)
            {
                case MicrowaveVisualState.Broken:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mwb");
                    if(microwaveComponent != null)
                        microwaveSystem.StopSoundLoop(microwaveComponent);
                    break;

                case MicrowaveVisualState.Idle:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_unlit");
                    if(microwaveComponent != null)
                        microwaveSystem.StopSoundLoop(microwaveComponent);
                    break;

                case MicrowaveVisualState.Cooking:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_running_unlit");
                    if(microwaveComponent != null)
                        microwaveSystem.StartSoundLoop(microwaveComponent);
                    break;

                default:
                    Logger.Debug($"Something terrible happened in {this}");
                    break;

            }

            var glowingPartsVisible = !(component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && !powered);
            sprite.LayerSetVisible(MicrowaveVisualizerLayers.BaseUnlit, glowingPartsVisible);
        }

        private enum MicrowaveVisualizerLayers : byte
        {
            Base,
            BaseUnlit
        }
    }


}
