
using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Client.GameObjects;
using System;

namespace Content.Client.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a handheld glowstick which can be activated and eventually dies over time.
    /// </summary>
    [RegisterComponent]
    internal sealed class ExpendableLightComponent : SharedExpendableLightComponent
    {
        private float _expiryTime = default;
        private float _fullExpiryTime = default;
        private PointLightComponent _light = default;

        public void Update(float frameTime)
        {
            if (CurrentState == LightState.Fading && _light != null && _expiryTime >= 0f)
            {
                var fade = MathF.Max(_expiryTime / _fullExpiryTime, 0.02f);

                _light.Energy = fade * GlowEnergy;
                _light.Radius = 2f + fade * (GlowRadius - 2f);

                _expiryTime -= frameTime;
            }
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is ExpendableLightComponentState state))
            {
                return;
            }

            Owner.TryGetComponent(out _light);

            CurrentState = state.State;
            _expiryTime = state.StateExpiryTime;
            _fullExpiryTime = state.StateExpiryTime;

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                switch (CurrentState)
                {
                    default:
                    case LightState.BrandNew:
                        break;

                    case LightState.Lit:
                    case LightState.Fading:

                        ToggleLight(enabled: true);
                        sprite.LayerSetState(1, IconStateLit);

                        break;

                    case LightState.Dead:

                        ToggleLight(enabled: false);
                        sprite.LayerSetState(1, IconStateSpent);

                        break;
                }
            }
        }

        private void ToggleLight(bool enabled)
        {
            if (Owner.TryGetComponent<AppearanceComponent>(out var appearance) && appearance.TryGetVisualizer<LightBehaviourVisualizer>(out var visualizer))
            {
                if (enabled)
                {
                    visualizer.StartLightBehaviour();
                }
                else
                {
                    visualizer.StopLightBehaviour();
                    _light.Enabled = false;
                }
            }
        }
    }
}
