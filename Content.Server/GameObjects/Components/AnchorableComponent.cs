#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Pulling;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
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

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<PhysicsComponent>(out _physicsComponent);
        }

        /// <summary>
        ///     Checks if a tool can change the anchored status.
        /// </summary>
        /// <param name="user">The user doing the action</param>
        /// <param name="utilizing">The tool being used, can be null if forcing it</param>
        /// <param name="force">Whether or not to check if the tool is valid</param>
        /// <returns>true if it is valid, false otherwise</returns>
        private async Task<bool> Valid(IEntity? user, IEntity? utilizing, [NotNullWhen(true)] bool force = false)
        {
            if (!Owner.HasComponent<IPhysBody>())
            {
                return false;
            }

            if (user != null && !force)
            {
                if (utilizing == null ||
                    !utilizing.TryGetComponent(out ToolComponent? tool) ||
                    !(await tool.UseTool(user, Owner, 0.5f, Tool)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Tries to anchor the owner of this component.
        /// </summary>
        /// <param name="user">The entity doing the anchoring</param>
        /// <param name="utilizing">The tool being used, if any</param>
        /// <param name="force">Whether or not to ignore valid tool checks</param>
        /// <returns>true if anchored, false otherwise</returns>
        public async Task<bool> TryAnchor(IEntity? user, IEntity? utilizing = null, bool force = false)
        {
            if (!(await Valid(user, utilizing, force)))
            {
                return false;
            }

            if (_physicsComponent == null)
                return false;

            var attempt = new AnchorAttemptMessage();

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, attempt, false);

            if (attempt.Cancelled)
                return false;

            _physicsComponent.BodyType = BodyType.Static;

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
                Owner.SnapToGrid(SnapGridOffset.Center, Owner.EntityManager);

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new AnchoredMessage(), false);

            return true;
        }

        /// <summary>
        ///     Tries to unanchor the owner of this component.
        /// </summary>
        /// <param name="user">The entity doing the unanchoring</param>
        /// <param name="utilizing">The tool being used, if any</param>
        /// <param name="force">Whether or not to ignore valid tool checks</param>
        /// <returns>true if unanchored, false otherwise</returns>
        public async Task<bool> TryUnAnchor(IEntity? user, IEntity? utilizing = null, bool force = false)
        {
            if (!(await Valid(user, utilizing, force)))
            {
                return false;
            }

            if (_physicsComponent == null)
                return false;

            var attempt = new UnanchorAttemptMessage();

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, attempt, false);

            if (attempt.Cancelled)
                return false;

            _physicsComponent.BodyType = BodyType.Dynamic;

            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UnanchoredMessage(), false);

            return true;
        }

        /// <summary>
        ///     Tries to toggle the anchored status of this component's owner.
        /// </summary>
        /// <param name="user">The entity doing the unanchoring</param>
        /// <param name="utilizing">The tool being used, if any</param>
        /// <param name="force">Whether or not to ignore valid tool checks</param>
        /// <returns>true if toggled, false otherwise</returns>
        public async Task<bool> TryToggleAnchor(IEntity? user, IEntity? utilizing = null, bool force = false)
        {
            if (_physicsComponent == null)
                return false;

            return _physicsComponent.BodyType == BodyType.Static ?
                await TryUnAnchor(user, utilizing, force) :
                await TryAnchor(user, utilizing, force);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return await TryToggleAnchor(eventArgs.User, eventArgs.Using);
        }
    }

    public class AnchorAttemptMessage : CancellableEntityEventArgs { }
    public class UnanchorAttemptMessage : CancellableEntityEventArgs { }

    public class AnchoredMessage : EntityEventArgs {}
    public class UnanchoredMessage : EntityEventArgs {}
}
