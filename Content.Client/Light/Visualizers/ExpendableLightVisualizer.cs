using Content.Client.Light.Components;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Light.Visualizers
{
    [UsedImplicitly]
    public sealed class ExpendableLightVisualizer : AppearanceVisualizer
    {
        [DataField("iconStateSpent")]
        public string? IconStateSpent { get; set; }

        [DataField("iconStateOn")]
        public string? IconStateLit { get; set; }

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();

            if (!entities.TryGetComponent(component.Owner, out ExpendableLightComponent? expendableLight))
                    return;

            if (!entities.TryGetComponent(component.Owner, out SpriteComponent? sprite))
                    return;

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

            if (!component.TryGetData(ExpendableLightVisuals.State, out ExpendableLightState state))
                return;

            switch (state)
            {
                case ExpendableLightState.Lit:
                    expendableLight.PlayingStream?.Stop();
                    expendableLight.PlayingStream = entities.EntitySysManager.GetEntitySystem<SharedAudioSystem>().PlayPvs(
                        expendableLight.LoopedSound,
                        expendableLight.Owner,
                        SharedExpendableLightComponent.LoopedSoundParams);
                    if (!string.IsNullOrWhiteSpace(IconStateLit))
                    {
                        sprite.LayerSetState(2, IconStateLit);
                        sprite.LayerSetShader(2, "shaded");
                    }

                    sprite.LayerSetVisible(1, true);

                    break;
                case ExpendableLightState.Dead:
                    expendableLight.PlayingStream?.Stop();
                    if (!string.IsNullOrWhiteSpace(IconStateSpent))
                    {
                        sprite.LayerSetState(0, IconStateSpent);
                        sprite.LayerSetShader(0, "shaded");
                    }

                    sprite.LayerSetVisible(1, false);
                    break;
            }
        }
    }
}
