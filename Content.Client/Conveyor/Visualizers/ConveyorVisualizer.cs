using System;
using Content.Shared.Conveyor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Conveyor.Visualizers
{
    [UsedImplicitly]
    public class ConveyorVisualizer : AppearanceVisualizer
    {
        [DataField("state_running")]
        private string? _stateRunning;

        [DataField("state_stopped")]
        private string? _stateStopped;

        [DataField("state_reversed")]
        private string? _stateReversed;

        private void ChangeState(AppearanceComponent appearance)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(appearance.Owner, out ISpriteComponent? sprite))
            {
                return;
            }

            if (appearance.TryGetData(ConveyorVisuals.State, out ConveyorState state))
            {
                var texture = state switch
                {
                    ConveyorState.Off => _stateStopped,
                    ConveyorState.Forward => _stateRunning,
                    ConveyorState.Reversed => _stateReversed,
                    _ => throw new ArgumentOutOfRangeException()
                };

                sprite.LayerSetState(0, texture);
            }
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
