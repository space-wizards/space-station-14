using System;
using Content.Shared.Disposal.Components;
using Content.Shared.SubFloor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Disposal.Visualizers
{
    [UsedImplicitly]
    public class DisposalVisualizer : AppearanceVisualizer
    {
        [DataField("state_free")]
        private string? _stateFree;

        [DataField("state_anchored")]
        private string? _stateAnchored;

        [DataField("state_broken")]
        private string? _stateBroken;

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            if (!appearance.TryGetData(DisposalTubeVisuals.VisualState, out DisposalTubeVisualState state))
            {
                return;
            }

            var texture = state switch
            {
                DisposalTubeVisualState.Free => _stateFree,
                DisposalTubeVisualState.Anchored => _stateAnchored,
                DisposalTubeVisualState.Broken => _stateBroken,
                _ => throw new ArgumentOutOfRangeException()
            };

            sprite.LayerSetState(0, texture);

            if (state == DisposalTubeVisualState.Anchored)
            {
                appearance.Owner.EnsureComponent<SubFloorHideComponent>();
            }
            else if (appearance.Owner.HasComponent<SubFloorHideComponent>())
            {
                appearance.Owner.RemoveComponent<SubFloorHideComponent>();
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
            ChangeState(component);
        }
    }
}
