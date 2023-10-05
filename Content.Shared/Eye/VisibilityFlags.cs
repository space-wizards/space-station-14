using Robust.Shared.Serialization;

namespace Content.Shared.Eye
{
    /// <summary>
    /// Flags used to filter which entities are visible to which players.
    /// This is done by checking if the client eye possesses _all_ of the entity's visibility flags.
    /// </summary>
    /// <remarks>
    /// PVS allocates chunks for each unique combination of these flags applied to a _client eye_.
    /// As such, the unique combinations of these flags applied to client eyes should be minimized.
    /// </remarks>
    [Flags]
    [FlagsFor(typeof(VisibilityMaskLayer))]
    public enum VisibilityFlags : int
    {
        /// <summary>
        /// On entities this indicates that no PVS visibility filtering should be applied to the entity.
        /// On clients this indicates that the client can't see any entities with PVS visibility filtering enabled.
        /// </summary>
        None   = 0,
        /// <summary>
        /// The default visibility flag, and mask, for entities and client eyes.
        /// </summary>
        /// <remarks>
        /// Some client systems operate under the assumption that this is present on all entities.
        /// TODO: Maybe consider obsoleting this? Client eyes seem to always possess this flag so all clients can see all normal entities. Is there any condition in which we want clients to _only_ see ghosts or something like that?
        /// </remarks>
        Normal = 1 << 0,
        /// <summary>
        /// The PVS visibility flag that should be applied to ghosts and things only visible to ghosts.
        /// </summary>
        Ghost  = 1 << 1,

        /// <summary>
        /// A PVS visibility flag that should render any entity with it invisible by default to _all clients_.
        /// </summary>
        /// <remarks><see cref="EyeComponent"/>s should _never_ have this PVS flag set.</remarks>
        PvsIgnore = 1 << 31,
    }
}
