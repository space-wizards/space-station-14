using Content.Shared.Roles;
using Content.Shared.Whitelist;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that requires minds to have a role matching a whitelist.
/// </summary>
public sealed partial class HasRoleMindFilter : MindFilter
{
    /// <summary>
    /// The whitelist a role must match for the mind to pass the filter.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var roleSys = entMan.System<SharedRoleSystem>();
        return !roleSys.MindHasRole(mind, Whitelist);
    }
}
