using Content.Server.Roles;

namespace Content.Server.Suspicion.Roles
{
    public abstract class SuspicionRole : Role
    {
        protected SuspicionRole(Mind.Mind mind) : base(mind) { }
    }
}
