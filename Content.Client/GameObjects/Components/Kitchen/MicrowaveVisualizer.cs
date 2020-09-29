using Content.Client.GameObjects.Components.Sound;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.Components.Sound;
using Content.Shared.Kitchen;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Audio;
using Robust.Shared.Log;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public sealed class MicrowaveVisualizer : AppearanceVisualizer
    {
        private LoopingSoundComponent _loopingSoundComponent;

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            _loopingSoundComponent ??= component.Owner.GetComponent<LoopingSoundComponent>();
            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out MicrowaveVisualState state))
            {
                state = MicrowaveVisualState.Idle;
            }
            switch (state)
            {
                case MicrowaveVisualState.Idle:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_unlit");
                    _loopingSoundComponent.StopAllSounds();
                    break;

                case MicrowaveVisualState.Cooking:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_running_unlit");
                    var audioParams = AudioParams.Default;
                    audioParams.Loop = true;
                    var scheduledSound = new ScheduledSound();
                    scheduledSound.Filename = "/Audio/Machines/microwave_loop.ogg";
                    scheduledSound.AudioParams = audioParams;
                    _loopingSoundComponent.StopAllSounds();
                    _loopingSoundComponent.AddScheduledSound(scheduledSound);
                    break;

                default:
                    Logger.Debug($"Something terrible happened in {this}");
                    break;

            }

            var glowingPartsVisible = !(component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) && !powered);
            sprite.LayerSetVisible(MicrowaveVisualizerLayers.BaseUnlit, glowingPartsVisible);
        }

        private enum MicrowaveVisualizerLayers
        {
            Base,
            BaseUnlit
        }
    }


}
