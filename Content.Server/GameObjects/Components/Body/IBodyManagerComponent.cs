using System;
using Content.Server.Body;
using Content.Server.Body.Network;
using Content.Shared.GameObjects.Components.Body;

namespace Content.Server.GameObjects.Components.Body
{
    // TODO: Merge with ISharedBodyManagerComponent
    public interface IBodyManagerComponent : ISharedBodyManagerComponent
    {
        /// <summary>
        ///     The <see cref="BodyTemplate"/> that this <see cref="BodyManagerComponent"/>
        ///     is adhering to.
        /// </summary>
        public BodyTemplate Template { get; }

        /// <summary>
        ///     The <see cref="BodyPreset"/> that this <see cref="BodyManagerComponent"/>
        ///     is adhering to.
        /// </summary>
        public BodyPreset Preset { get; }

        /// <summary>
        ///     Installs the given <see cref="IBodyPart"/> into the given slot.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        bool TryAddPart(string slot, IBodyPart part, bool force = false);

        bool HasPart(string slot);

        /// <summary>
        ///     Ensures that this body has the specified network.
        /// </summary>
        /// <typeparam name="T">The type of the network to ensure.</typeparam>
        /// <returns>
        ///     True if the network already existed, false if it had to be created.
        /// </returns>
        bool EnsureNetwork<T>() where T : BodyNetwork;

        /// <summary>
        ///     Ensures that this body has the specified network.
        /// </summary>
        /// <param name="networkType">The type of the network to ensure.</param>
        /// <returns>
        ///     True if the network already existed, false if it had to be created.
        /// </returns>
        bool EnsureNetwork(Type networkType);

        /// <summary>
        ///     Removes the <see cref="BodyNetwork"/> of the given type in this body,
        ///     if one exists.
        /// </summary>
        /// <typeparam name="T">The type of the network to remove.</typeparam>
        void RemoveNetwork<T>() where T : BodyNetwork;

        /// <summary>
        ///     Removes the <see cref="BodyNetwork"/> of the given type in this body,
        ///     if there is one.
        /// </summary>
        /// <param name="networkType">The type of the network to remove.</param>
        void RemoveNetwork(Type networkType);

        void PreMetabolism(float frameTime);

        void PostMetabolism(float frameTime);
    }
}
