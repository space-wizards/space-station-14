using Robust.Client.GameObjects;
using Content.Shared.Hands;
using Content.Shared.Collapsible;
using Robust.Shared.Containers;
using Content.Shared.Item;

namespace Content.Client.Collapsible
{
    public sealed class CollapsibleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollapsibleVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals);
        }

        private void OnGetHeldVisuals(EntityUid uid, CollapsibleVisualsComponent component, GetInhandVisualsEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && TryComp(uid, out AppearanceComponent? appearance)
                && appearance.TryGetData(CollapsibleVisuals.InhandsVisible, out bool visible))
            {
                if (!visible)
                {
                    args.Layers.Clear();
                }
            }
        }
    }

    public sealed class CollapsibleVisualsSystem : VisualizerSystem<CollapsibleVisualsComponent>
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        protected override void OnAppearanceChange(EntityUid uid, CollapsibleVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(CollapsibleVisuals.IsCollapsed, out bool isCollapsed))
            {
                var state = isCollapsed ? component.CollapsedState : component.ExtendedState;
                sprite.LayerSetState(CollapsibleVisualLayers.IsCollapsed, state);
            }
            if (_containerSystem.TryGetContainingContainer(uid, out var container))
                RaiseLocalEvent(container.Owner, new VisualsChangedEvent(uid, container.ID));
        }
    }
}

public enum CollapsibleVisualLayers : byte
{
    IsCollapsed,
    InhandsVisible
}
