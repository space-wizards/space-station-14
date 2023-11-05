using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind entities to tag that they are a nuke operative.
/// </summary>
[RegisterComponent]
public sealed partial class NukeopsRoleComponent : AntagonistRoleComponent
{
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/nukeops_start.ogg");
}
