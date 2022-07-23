using Content.Server.Roles;

namespace Content.Server.Ghost.Roles
{
    /// <summary>
    /// This is used for round end display of ghost roles.
    /// It may also be used to ensure some ghost roles count as antagonists in future.
    /// </summary>
    public sealed class GhostRoleMarkerRole : Role
    {
        private readonly string _name;
        public override string Name => _name;
        public override bool Antagonist => false;

        public GhostRoleMarkerRole(Mind.Mind mind, string name) : base(mind)
        {
            _name = name;
        }
    }
}
