using System;
using System.Reflection.Metadata.Ecma335;
using Content.Client.GameObjects.Components.Sound;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.Components.Sound;
using Content.Shared.Kitchen;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Kitchen
{
    public sealed class MicrowaveVisualizer : AppearanceVisualizer
    {
        private SoundComponent _soundComponent;
        private const string _microwaveSoundLoop = "/Audio/machines/microwave_loop.ogg";
        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            _soundComponent ??= component.Owner.GetComponent<SoundComponent>();
            if (!component.TryGetData(PowerDeviceVisuals.VisualState, out MicrowaveVisualState state))
            {
                state = MicrowaveVisualState.Idle;
            }
            switch (state)
            {
                case MicrowaveVisualState.Idle:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_unlit");
                    _soundComponent.StopAllSounds();
                    break;

                case MicrowaveVisualState.Cooking:
                    sprite.LayerSetState(MicrowaveVisualizerLayers.Base, "mw");
                    sprite.LayerSetState(MicrowaveVisualizerLayers.BaseUnlit, "mw_running_unlit");
                    var audioParams = AudioParams.Default;
                    audioParams.Loop = true;
                    var schedSound = new ScheduledSound();
                    schedSound.Filename = _microwaveSoundLoop;
                    schedSound.AudioParams = audioParams;
                    _soundComponent.AddScheduledSound(schedSound);
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
