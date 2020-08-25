
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
                var fade = MathF.Max(_expiryTime / _fullExpiryTime, 0.08f);

                _light.Energy = fade * 2.2f;
                _light.Radius = 2.0f + fade * 1.0f;

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

                        if (Owner.TryGetComponent<LightBehaviourComponent>(out var lightBehaviour))
                        {
                            lightBehaviour.StartLightBehaviour();
                        }

                        sprite.LayerSetState(1, IconStateLit);
                        break;

                    case LightState.Dead:

                        if (Owner.TryGetComponent<LightBehaviourComponent>(out var light))
                        {
                            light.StopLightBehaviour();
                        }

                        _light.Enabled = false;

                        sprite.LayerSetState(1, IconStateSpent);
                        break;
                }
            }
        }
    }
}
