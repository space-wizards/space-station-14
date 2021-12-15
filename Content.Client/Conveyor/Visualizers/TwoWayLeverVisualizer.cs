using Content.Shared.MachineLinking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Conveyor.Visualizers
{
    [UsedImplicitly]
    public class TwoWayLeverVisualizer : AppearanceVisualizer
    {
        [DataField("state_forward")]
        private string? _stateForward;

        [DataField("state_off")]
        private string? _stateOff;

        [DataField("state_reversed")]
        private string? _stateReversed;

        private void ChangeState(AppearanceComponent appearance)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(appearance.Owner, out ISpriteComponent? sprite))
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

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);

            var entities = IoCManager.Resolve<IEntityManager>();
            var appearance = entities.EnsureComponent<ClientAppearanceComponent>(entity);
            ChangeState(appearance);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            ChangeState(component);
        }
    }
}
