using System;
using Content.Shared.GameObjects.Components.Disposal;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.Disposal
{
    [UsedImplicitly]
    public class DisposalVisualizer : AppearanceVisualizer
    {
        private string _stateFree;
        private string _stateAnchored;
        private string _stateBroken;

        private void ChangeState(AppearanceComponent appearance)
        {
            if (!appearance.Owner.TryGetComponent(out ISpriteComponent sprite))
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

        public override void LoadData(YamlMappingNode node)
        {
            base.LoadData(node);

            _stateFree = node.GetNode("state_free").AsString();
            _stateAnchored = node.GetNode("state_anchored").AsString();
            _stateBroken = node.GetNode("state_broken").AsString();
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
