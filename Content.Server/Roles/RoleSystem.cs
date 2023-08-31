using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class RoleSystem : SharedRoleSystem
{
    public override void Initialize()
    {
        // TODO make roles entities
        base.Initialize();

        SubscribeAntagEvents<NukeopsRoleComponent>();
        SubscribeAntagEvents<SubvertedSiliconRoleComponent>();
        SubscribeAntagEvents<TraitorRoleComponent>();
        SubscribeAntagEvents<ZombieRoleComponent>();
    }

    public string? MindGetBriefing(EntityUid? mindId)
    {
        // TODO this should be an event
        return CompOrNull<TraitorRoleComponent>(mindId)?.Briefing;
    }
}
