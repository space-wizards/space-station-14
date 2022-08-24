using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Client.Light.Visualizers
{
    [UsedImplicitly]
    public sealed class ExpendableLightVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (component.TryGetData(ExpendableLightVisuals.Behavior, out string lightBehaviourID))
            {
                if (entities.TryGetComponent(component.Owner, out LightBehaviourComponent? lightBehaviour))
                {
                    lightBehaviour.StopLightBehaviour();

                    if (lightBehaviourID != string.Empty)
                    {
                        lightBehaviour.StartLightBehaviour(lightBehaviourID);
                    }
                    else if (entities.TryGetComponent(component.Owner, out PointLightComponent? light))
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
            && entities.TryGetComponent(component.Owner, out ExpendableLightComponent? expendableLight))
            {
                switch (state)
                {
                    case ExpendableLightState.Lit:
                    {
                        TryStopStream(expendableLight.PlayingStream);
                        if (expendableLight.LoopedSound != null)
                        {
                            expendableLight.PlayingStream = SoundSystem.Play(expendableLight.LoopedSound, Filter.Local(),
                                expendableLight.Owner, SharedExpendableLightComponent.LoopedSoundParams.WithLoop(true));
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
