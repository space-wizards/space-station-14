
using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Client.GameObjects;

namespace Content.Client.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a handheld expendable light which can be activated and eventually dies over time.
    /// </summary>
    [RegisterComponent]
    public class ExpendableLightComponent : SharedExpendableLightComponent
    {
        private PointLightComponent _light = default;
        private LightBehaviourComponent _lightBehaviour = default;

        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out _lightBehaviour);
            Owner.TryGetComponent(out _light);
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is ExpendableLightComponentState state))
            {
                return;
            }

            CurrentState = state.State;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            switch (CurrentState)
            {
                default:
                case LightState.BrandNew:
                    break;

                case LightState.Lit:

                    _lightBehaviour.StartLightBehaviour(TurnOnBehaviourID);
                    break;

                case LightState.Fading:

                    _lightBehaviour.StopLightBehaviour(TurnOnBehaviourID);
                    _lightBehaviour.StartLightBehaviour(FadeOutBehaviourID);
                    break;

                case LightState.Dead:

                    _lightBehaviour.StopLightBehaviour();
                    _light.Enabled = false;
                    break;
            }
        }
    }
}
