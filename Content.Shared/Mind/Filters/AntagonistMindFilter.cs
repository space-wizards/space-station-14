using Content.Shared.Roles;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that requires minds to have an antagonist role.
/// </summary>
public sealed partial class AntagonistMindFilter : MindFilter
{
    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var roleSys = entMan.System<SharedRoleSystem>();
        return !roleSys.MindIsAntagonist(mind);
    }
}
