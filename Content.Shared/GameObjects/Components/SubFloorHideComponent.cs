#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Shared.GameObjects.Components
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
            if (_snapGridComponent == null)
            {
                // Shouldn't happen but allows us to use nullables. OnPositionChanged needs to be componentbus anyway.
                Logger.Error("Snapgrid was null for subfloor {Owner}");
                return;
            }
            _snapGridComponent.OnPositionChanged += SnapGridOnPositionChanged;
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
