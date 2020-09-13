using System;
using Content.Server.Body;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Server.GameObjects.Components.Body
{
    // TODO: Merge with ISharedBodyManagerComponent
    public interface IBodyManager : ISharedBodyManager, IBodyPartManager
    {
        /// <summary>
        ///     The <see cref="BodyTemplate"/> that this
        ///     <see cref="BodyManagerComponent"/> is adhering to.
        /// </summary>
        public BodyTemplate Template { get; }

        /// <summary>
        ///     Installs the given <see cref="ISharedBodyPart"/> into the given slot.
        /// </summary>
        /// <returns>True if successful, false otherwise.</returns>
        bool TryAddPart(string slot, ISharedBodyPart part, bool force = false);

        bool HasPart(string slot);
    }
}
