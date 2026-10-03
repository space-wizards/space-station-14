using Content.Shared.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Notes;

public abstract partial class AdminNotesSystem : EntitySystem
{
    [Dependency] private ISharedAdminManager _admins = default!;

    [SubscribeLocalEvent]
    private void AddVerbs(GetVerbsEvent<Verb> ev)
    {
        if (!HasComp<ActorComponent>(ev.User) || !HasComp<ActorComponent>(ev.Target) ||
            !_admins.HasAdminFlag(ev.User, AdminFlags.ViewNotes))
            return;

        var user = ev.User;
        var target = ev.Target;

        var verb = new Verb
        {
            Text = Loc.GetString("admin-notes-verb-text"),
            Category = VerbCategory.Admin,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png")),
            Act = () => OpenNotes(user, target),
            Impact = LogImpact.Low,
        };

        ev.Verbs.Add(verb);
    }

    protected virtual void OpenNotes(EntityUid user, EntityUid target) { }
}
