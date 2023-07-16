using Content.Server.Chat.Managers;
using Content.Shared.PDA;
using Content.Shared.Roles;

namespace Content.Server.Roles;

public sealed class TraitorRole : AntagonistRole
{
    public TraitorRole(Mind.Mind mind, AntagPrototype antagPrototype) : base(mind, antagPrototype) { }
}
