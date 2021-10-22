using System;
using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client.Light.Visualizers
{
    [UsedImplicitly]
    public class ExpendableLightVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData(ExpendableLightVisuals.Behavior, out string lightBehaviourID))
            {
                if (component.Owner.TryGetComponent<LightBehaviourComponent>(out var lightBehaviour))
                {
                    lightBehaviour.StopLightBehaviour();

                    if (lightBehaviourID != string.Empty)
                    {
                        lightBehaviour.StartLightBehaviour(lightBehaviourID);
                    }
                    else if (component.Owner.TryGetComponent<PointLightComponent>(out var light))
                    {
                        light.Enabled = false;
                    }
                }
            }

            void TryStopStream(IPlayingAudioStream? stream)
            {
                stream?.Stop();
            }

            if (component.TryGetData(ExpendableLightVisuals.State, out ExpendableLightState state)
            && component.Owner.TryGetComponent<ExpendableLightComponent>(out var expendableLight))
            {
                switch (state)
                {
                    case ExpendableLightState.Lit:
                    {
                        TryStopStream(expendableLight.PlayingStream);
                        if (expendableLight.LoopedSound != null)
                        {
                            expendableLight.PlayingStream = SoundSystem.Play(Filter.Local(),
                                expendableLight.LoopedSound, expendableLight.Owner,
                                SharedExpendableLightComponent.LoopedSoundParams.WithLoop(true));
                        }
                        break;
                    }
                    case ExpendableLightState.Dead:
                    {
                        TryStopStream(expendableLight.PlayingStream);
                        break;
                    }
                }
            }
        }
    }
}
