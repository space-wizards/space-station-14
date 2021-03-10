using Content.Shared.GameObjects.Components.MachineLinking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Conveyor
{
    [UsedImplicitly]
    public class TwoWayLeverVisualizer : AppearanceVisualizer
    {
        [DataField("state_forward")]
        private string _stateForward;
        [DataField("state_off")]
        private string _stateOff;
        [DataField("state_reversed")]
        private string _stateReversed;

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            appearance.TryGetData(TwoWayLeverVisuals.State, out TwoWayLeverSignal state);

            var texture = state switch
            {
                TwoWayLeverSignal.Middle => _stateOff,
                TwoWayLeverSignal.Right => _stateForward,
                TwoWayLeverSignal.Left => _stateReversed,
                _ => _stateOff
            };

            sprite.LayerSetState(0, texture);
        }

        public override void InitializeEntity(IEntity entity)
        {
            base.InitializeEntity(entity);

            var appearance = entity.EnsureComponent<AppearanceComponent>();
            ChangeState(appearance);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            ChangeState(component);
        }
    }
}
