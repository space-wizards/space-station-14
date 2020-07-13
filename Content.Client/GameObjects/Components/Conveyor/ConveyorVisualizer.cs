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

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            appearance.TryGetData(ConveyorVisuals.State, out ConveyorState state);

            var texture = state switch
            {
                ConveyorState.Stopped => _stateStopped,
                ConveyorState.Running => _stateRunning,
                ConveyorState.Reversed => _stateReversed,
                _ => throw new ArgumentOutOfRangeException()
            };

            sprite.LayerSetState(0, texture);
        }

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            if (node.TryGetNode("state_running", out var child))
            {
                _stateRunning = child.AsString();
            }

            if (node.TryGetNode("state_stopped", out child))
            {
                _stateStopped = child.AsString();
            }

            if (node.TryGetNode("state_reversed", out child))
            {
                _stateReversed = child.AsString();
            }
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

            if (component.Owner.Deleted)
            {
                return;
            }

            ChangeState(component);
        }
    }
}
