using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

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
        private SnapGridComponent _snapGridComponent;

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

            _snapGridComponent.OnPositionChanged += SnapGridOnPositionChanged;
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new SubFloorHideDirtyEvent(Owner));
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            if(Owner.Transform.Running == false)
                return;

            _snapGridComponent.OnPositionChanged -= SnapGridOnPositionChanged;
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new SubFloorHideDirtyEvent(Owner));
        }

        private void SnapGridOnPositionChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new SubFloorHideDirtyEvent(Owner));
        }
    }

    internal sealed class SubFloorHideDirtyEvent : EntitySystemMessage
    {
        public IEntity Sender { get; }

        public SubFloorHideDirtyEvent(IEntity sender)
        {
            Sender = sender;
        }
    }
}
