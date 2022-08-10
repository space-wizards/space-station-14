using Content.Server.Administration.Commands;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Console;

namespace Content.Server.Administration.Notes;

public sealed class AdminNotesSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IAdminNotesManager _notes = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(AddVerbs);
    }

    private void AddVerbs(GetVerbsEvent<Verb> ev)
    {
        if (EntityManager.GetComponentOrNull<ActorComponent>(ev.User) is not {PlayerSession: var user} ||
            EntityManager.GetComponentOrNull<ActorComponent>(ev.Target) is not {PlayerSession: var target})
        {
            return;
        }

        if (!_notes.CanView(user))
        {
            return;
        }

        var verb = new Verb
        {
            Text = Loc.GetString("admin-notes-verb-text"),
            Category = VerbCategory.Admin,
            IconTexture = "/Textures/Interface/VerbIcons/examine.svg.192dpi.png",
            Act = () => _console.RemoteExecuteCommand(user, $"{OpenAdminNotesCommand.CommandName} \"{target.UserId}\""),
            Impact = LogImpact.Low
        };

        ev.Verbs.Add(verb);
    }
}
