#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class AnchorableComponent : Component, IInteractUsing
    {
        public override string Name => "Anchorable";

        /// <summary>
        ///     Checks if a tool can change the anchored status.
        /// </summary>
        /// <param name="user">The user doing the action</param>
        /// <param name="utilizing">The tool being used, can be null if forcing it</param>
        /// <param name="collidable">The physics component of the owning entity</param>
        /// <param name="force">Whether or not to check if the tool is valid</param>
        /// <returns>true if it is valid, false otherwise</returns>
        private bool Valid(IEntity user, IEntity? utilizing, [MaybeNullWhen(false)] out ICollidableComponent collidable, bool force = false)
        {
            if (!Owner.TryGetComponent(out collidable))
            {
                return false;
            }

            if (!force)
            {
                if (utilizing == null ||
                    !utilizing.TryGetComponent(out ToolComponent tool) ||
                    !tool.UseTool(user, Owner, ToolQuality.Anchoring))
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
        public bool TryAnchor(IEntity user, IEntity? utilizing = null, bool force = false)
        {
            if (!Valid(user, utilizing, out var physics, force))
            {
                return false;
            }

            physics.Anchored = true;

            return true;
        }

        /// <summary>
        ///     Tries to unanchor the owner of this component.
        /// </summary>
        /// <param name="user">The entity doing the unanchoring</param>
        /// <param name="utilizing">The tool being used, if any</param>
        /// <param name="force">Whether or not to ignore valid tool checks</param>
        /// <returns>true if unanchored, false otherwise</returns>
        public bool TryUnAnchor(IEntity user, IEntity? utilizing = null, bool force = false)
        {
            if (!Valid(user, utilizing, out var physics, force))
            {
                return false;
            }

            physics.Anchored = false;

            return true;
        }

        /// <summary>
        ///     Tries to toggle the anchored status of this component's owner.
        /// </summary>
        /// <param name="user">The entity doing the unanchoring</param>
        /// <param name="utilizing">The tool being used, if any</param>
        /// <param name="force">Whether or not to ignore valid tool checks</param>
        /// <returns>true if toggled, false otherwise</returns>
        private bool TryToggleAnchor(IEntity user, IEntity? utilizing = null, bool force = false)
        {
            if (!Owner.TryGetComponent(out ICollidableComponent collidable))
            {
                return false;
            }

            return collidable.Anchored ?
                TryUnAnchor(user, utilizing, force) :
                TryAnchor(user, utilizing, force);
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<CollidableComponent>();
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryToggleAnchor(eventArgs.User, eventArgs.Using);
        }
    }
}
