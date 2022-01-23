using System;
using Content.Shared.Disposal.Components;
using Content.Shared.SubFloor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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

        private void ChangeState(AppearanceComponent appearance)
        {
            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(appearance.Owner, out ISpriteComponent? sprite))
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
                _ => throw new ArgumentOutOfRangeException()
            };

            sprite.LayerSetState(0, texture);

            if (state == DisposalTubeVisualState.Anchored)
            {
                appearance.Owner.EnsureComponent<SubFloorHideComponent>();
            }
            else if (entities.HasComponent<SubFloorHideComponent>(appearance.Owner))
            {
                entities.RemoveComponent<SubFloorHideComponent>(appearance.Owner);
            }
        }

        public override void InitializeEntity(EntityUid entity)
        {
            base.InitializeEntity(entity);
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var appearance = entityManager.EnsureComponent<ClientAppearanceComponent>(entity);
            ChangeState(appearance);
        }

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);
            ChangeState(component);
        }
    }
}
