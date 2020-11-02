using System;
using Content.Shared.GameObjects.Components.Conveyor;
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
    public class ConveyorVisualizer : AppearanceVisualizer
    {
        private string _stateRunning;
        private string _stateStopped;
        private string _stateReversed;
        private string _stateLoose;

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            appearance.TryGetData(ConveyorVisuals.State, out ConveyorState state);

            var texture = state switch
            {
                ConveyorState.Off => _stateStopped,
                ConveyorState.Forward => _stateRunning,
                ConveyorState.Reversed => _stateReversed,
                ConveyorState.Loose => _stateLoose,
                _ => throw new ArgumentOutOfRangeException()
            };

            sprite.LayerSetState(0, texture);
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _stateRunning = node.GetNode("state_running").AsString();
            _stateStopped = node.GetNode("state_stopped").AsString();
            _stateReversed = node.GetNode("state_reversed").AsString();
            _stateLoose = node.GetNode("state_loose").AsString();
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
