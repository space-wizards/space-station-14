using System;
using Content.Server.Roles;
using Robust.Shared.GameObjects;

namespace Content.Server.Mind.Systems
{
    /// <summary>
    ///     Manage players mind roles
    /// </summary>
    public class RolesSystem : EntitySystem
    {
        /// <summary>
        ///     Gives this mind a new role.
        /// </summary>
        /// <param name="mind">Mind to add a new role</param>
        /// <param name="role">The type of the role to give.</param>
        /// <returns>The instance of the role.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if we already have a role with this type.
        /// </exception>
        public Role AddRole(Mind mind, Role role)
        {
            if (mind._roles.Contains(role))
            {
                throw new ArgumentException($"We already have this role: {role}");
            }

            mind._roles.Add(role);
            role.Greet();

            var message = new RoleAddedEvent(role);
            mind.OwnedEntity?.EntityManager.EventBus.RaiseLocalEvent(mind.OwnedEntity.Uid, message);

            return role;
        }

        /// <summary>
        ///     Removes a role from this mind.
        /// </summary>
        /// <param name="mind">Mind to remove a new role</param>
        /// <param name="role">The type of the role to remove.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if we do not have this role.
        /// </exception>
        public void RemoveRole(Mind mind, Role role)
        {
            if (!mind._roles.Contains(role))
            {
                throw new ArgumentException($"We do not have this role: {role}");
            }

            mind._roles.Remove(role);

            var message = new RoleRemovedEvent(role);
            mind.OwnedEntity?.EntityManager.EventBus.RaiseLocalEvent(mind.OwnedEntity.Uid, message);
        }

    }
}
