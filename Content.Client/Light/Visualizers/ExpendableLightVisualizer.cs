using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

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

            if (component.TryGetData(ExpendableLightVisuals.State, out ExpendableLightState state)
                && entities.TryGetComponent(component.Owner, out ExpendableLightComponent? expendableLight))
            {
                switch (state)
                {
                    case ExpendableLightState.Lit:
                        expendableLight.PlayingStream?.Stop();
                        expendableLight.PlayingStream = entities.EntitySysManager.GetEntitySystem<SharedAudioSystem>().PlayPvs(
                            expendableLight.LoopedSound,
                            expendableLight.Owner,
                            SharedExpendableLightComponent.LoopedSoundParams);
                        break;
                    case ExpendableLightState.Dead:
                        expendableLight.PlayingStream?.Stop();
                        break;
                }
            }
        }
    }
}
