using Robust.Shared.Network;

namespace Content.Server.Roles
{
    /// <summary>
    ///     Provides special hooks for when jobs get spawned in/equipped.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class JobRequirement
    {
        public abstract bool RequirementFulfilled(NetUserId id);
    }
}
