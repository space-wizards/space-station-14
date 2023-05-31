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
        private bool _antagonist;
        public override bool Antagonist => _antagonist;

        public GhostRoleMarkerRole(Mind.Mind mind, string name, bool antagonist = false) : base(mind)
        {
            _name = name;
            _antagonist = antagonist;
        }
    }
}
