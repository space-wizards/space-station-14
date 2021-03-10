using System.Diagnostics;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.GameObjects.Components
{
    /// <summary>
    /// Simple component that automatically hides the sibling
    /// <see cref="ISpriteComponent" /> when the tile it's on is not a sub floor
    /// (plating).
    /// </summary>
    /// <seealso cref="P:Content.Shared.Maps.ContentTileDefinition.IsSubFloor" />
    [RegisterComponent]
    public sealed class SubFloorHideComponent : Component
    {
        [ComponentDependency(nameof(OnAddSnapGrid))]
        private SnapGridComponent? _snapGridComponent;

        /// <inheritdoc />
        public override string Name => "SubFloorHide";

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            _snapGridComponent = Owner.GetComponent<SnapGridComponent>();
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new SubFloorHideDirtyEvent(Owner));
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            if (Owner.Transform.Running == false)
                return;

            if (_snapGridComponent != null)
            {
                _snapGridComponent.OnPositionChanged -= SnapGridOnPositionChanged;
            }

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new SubFloorHideDirtyEvent(Owner));
        }

        private void OnAddSnapGrid()
        {
            DebugTools.AssertNotNull(_snapGridComponent);
            _snapGridComponent!.OnPositionChanged += SnapGridOnPositionChanged;
        }

        private void SnapGridOnPositionChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new SubFloorHideDirtyEvent(Owner));
        }
    }

    internal sealed class SubFloorHideDirtyEvent : EntityEventArgs
    {
        public IEntity Sender { get; }

        public SubFloorHideDirtyEvent(IEntity sender)
        {
            Sender = sender;
        }
    }
}
