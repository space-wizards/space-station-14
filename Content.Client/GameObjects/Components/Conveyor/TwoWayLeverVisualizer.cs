using System;
using Content.Shared.GameObjects.Components.Conveyor;
using Content.Shared.GameObjects.Components.MachineLinking;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Conveyor
{
    [UsedImplicitly]
    public class TwoWayLeverVisualizer : AppearanceVisualizer
    {
        private string _stateForward;
        private string _stateOff;
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

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _stateForward = node.GetNode("state_forward").AsString();
            _stateOff = node.GetNode("state_off").AsString();
            _stateReversed = node.GetNode("state_reversed").AsString();
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
