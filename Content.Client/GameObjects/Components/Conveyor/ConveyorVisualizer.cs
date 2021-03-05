using System;
using Content.Shared.GameObjects.Components.Conveyor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Conveyor
{
    [UsedImplicitly]
    public class ConveyorVisualizer : AppearanceVisualizer
    {
        [DataField("state_running")]
        private string _stateRunning;
        [DataField("state_stopped")]
        private string _stateStopped;
        [DataField("state_reversed")]
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
                ConveyorState.Off => _stateStopped,
                ConveyorState.Forward => _stateRunning,
                ConveyorState.Reversed => _stateReversed,
                _ => throw new ArgumentOutOfRangeException()
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
