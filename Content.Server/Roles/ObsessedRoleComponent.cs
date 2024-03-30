using Content.Shared.Roles;
namespace Content.Server.Roles;

// We dont want an obsessed that's also a syndie, or a rev/nukie
//      people want more antags and chaos, so this will hopefull add more depth to the game (more chaos = more depth)
[RegisterComponent, ExclusiveAntagonist]
public sealed partial class ObsessedRoleComponent : AntagonistRoleComponent
{
    // The person that we're obsessed with
    public EntityUid? TargetofAffection;
    // The person we need to kill (we're jealous of them)
    public EntityUid? TargetofJealousy;
}
