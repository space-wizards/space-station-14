using Content.Client.Sound;
using Content.Shared.Kitchen.Components;
using Content.Shared.Power;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
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

            var loopingSoundComponent = component.Owner.GetComponentOrNull<LoopingSoundComponent>();

            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out MicrowaveVisualState state))
            {
                state = MicrowaveVisualState.Idle;
            }
            switch (state)
            {
                case MicrowaveVisualState.Idle:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_unlit");
                    loopingSoundComponent?.StopAllSounds();
                    break;

                case MicrowaveVisualState.Cooking:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_running_unlit");
                    var audioParams = AudioParams.Default;
                    audioParams.Loop = true;
                    var scheduledSound = new ScheduledSound();
                    scheduledSound.Filename = "/Audio/Machines/microwave_loop.ogg";
                    scheduledSound.AudioParams = audioParams;
                    loopingSoundComponent?.StopAllSounds();
                    loopingSoundComponent?.AddScheduledSound(scheduledSound);
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
