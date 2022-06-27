using Robust.Shared.Network;

namespace Content.Shared.Roles
{
    /// <summary>
    ///     Abstract class for playtime and other requirements for role gates.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class JobRequirement
    {
        /// <summary>
        /// Returns requirement status and a user facing string to state how much time remains.
        /// </summary>
        /// <param name="id">The player's network id</param>
        /// <returns>A tuple of requirement status and a user facing string to state what they need to do to fulfill the requirement</returns>
        public abstract Tuple<bool, string?> GetRequirementStatus(NetUserId id);
    }
}
