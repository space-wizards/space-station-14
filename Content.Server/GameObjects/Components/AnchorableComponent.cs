#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Pulling;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    // TODO: Move this component's logic to an EntitySystem.
    [RegisterComponent]
    public class AnchorableComponent : Component, IInteractUsing
    {
        public override string Name => "Anchorable";

        [ComponentDependency] private PhysicsComponent? _physicsComponent = default!;

        [ViewVariables]
        [DataField("tool")]
        public ToolQuality Tool { get; private set; } = ToolQuality.Anchoring;

        [ViewVariables]
        int IInteractUsing.Priority => 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("snap")]
        public bool Snap { get; private set; } = true;

        /// <summary>
        ///     Checks if a tool can change the anchored status.
        /// </summary>
        /// <param name="user">The user doing the action</param>
        /// <param name="utilizing">The tool being used</param>
        /// <param name="anchoring">True if we're anchoring, and false if we're unanchoring.</param>
        /// <returns>true if it is valid, false otherwise</returns>
        private async Task<bool> Valid(IEntity user, IEntity utilizing, bool anchoring)
        {
            if (!Owner.HasComponent<IPhysBody>())
            {
                return false;
            }

            BaseAnchoredAttemptEvent attempt =
                anchoring ? new AnchorAttemptEvent(user, utilizing) : new UnanchorAttemptEvent(user, utilizing);

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, attempt, false);

            if (attempt.Cancelled)
                return false;

            return utilizing.TryGetComponent(out ToolComponent? tool) && await tool.UseTool(user, Owner, 0.5f, Tool);
        }

        /// <summary>
        ///     Tries to anchor the owner of this component.
        /// </summary>
        /// <param name="user">The entity doing the anchoring</param>
        /// <param name="utilizing">The tool being used</param>
        /// <returns>true if anchored, false otherwise</returns>
        public async Task<bool> TryAnchor(IEntity user, IEntity utilizing)
        {
            if (!(await Valid(user, utilizing, true)))
            {
                return false;
            }

            if (_physicsComponent == null)
                return false;

            // Snap rotation to cardinal (multiple of 90)
            var rot = Owner.Transform.LocalRotation;
            Owner.Transform.LocalRotation = Math.Round(rot / (Math.PI / 2)) * (Math.PI / 2);

            if (Owner.TryGetComponent(out PullableComponent? pullableComponent))
            {
                if (pullableComponent.Puller != null)
                {
                    pullableComponent.TryStopPull();
                }
            }

            if (Snap)
                Owner.SnapToGrid(Owner.EntityManager);

            _physicsComponent.BodyType = BodyType.Static;

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new AnchoredEvent(user, utilizing), false);

            return true;
        }

        /// <summary>
        ///     Tries to unanchor the owner of this component.
        /// </summary>
        /// <param name="user">The entity doing the unanchoring</param>
        /// <param name="utilizing">The tool being used, if any</param>
        /// <param name="force">Whether or not to ignore valid tool checks</param>
        /// <returns>true if unanchored, false otherwise</returns>
        public async Task<bool> TryUnAnchor(IEntity user, IEntity utilizing)
        {
            if (!(await Valid(user, utilizing, false)))
            {
                return false;
            }

            if (_physicsComponent == null)
                return false;

            _physicsComponent.BodyType = BodyType.Dynamic;

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UnanchoredEvent(user, utilizing), false);

            return true;
        }

        /// <summary>
        ///     Tries to toggle the anchored status of this component's owner.
        /// </summary>
        /// <param name="user">The entity doing the unanchoring</param>
        /// <param name="utilizing">The tool being used</param>
        /// <returns>true if toggled, false otherwise</returns>
        public async Task<bool> TryToggleAnchor(IEntity user, IEntity utilizing)
        {
            if (_physicsComponent == null)
                return false;

            return _physicsComponent.BodyType == BodyType.Static ?
                await TryUnAnchor(user, utilizing) :
                await TryAnchor(user, utilizing);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return await TryToggleAnchor(eventArgs.User, eventArgs.Using);
        }
    }

    public abstract class BaseAnchoredAttemptEvent : CancellableEntityEventArgs
    {
        public IEntity User { get; }
        public IEntity Tool { get; }

        protected BaseAnchoredAttemptEvent(IEntity user, IEntity tool)
        {
            User = user;
            Tool = tool;
        }
    }

    public class AnchorAttemptEvent : BaseAnchoredAttemptEvent
    {
        public AnchorAttemptEvent(IEntity user, IEntity tool) : base(user, tool) { }
    }

    public class UnanchorAttemptEvent : BaseAnchoredAttemptEvent
    {
        public UnanchorAttemptEvent(IEntity user, IEntity tool) : base(user, tool) { }
    }

    public abstract class BaseAnchoredEvent : EntityEventArgs
    {
        public IEntity User { get; }
        public IEntity Tool { get; }

        protected BaseAnchoredEvent(IEntity user, IEntity tool)
        {
            User = user;
            Tool = tool;
        }
    }

    public class AnchoredEvent : BaseAnchoredEvent
    {
        public AnchoredEvent(IEntity user, IEntity tool) : base(user, tool) { }
    }

    public class UnanchoredEvent : BaseAnchoredEvent
    {
        public UnanchoredEvent(IEntity user, IEntity tool) : base(user, tool) { }
    }
}
