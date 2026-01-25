using Content.Shared.Administration.Managers;
using Content.Shared.Animals.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Animals.Systems;

public abstract class SharedParrotMemorySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotMemoryComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<ParrotMemoryComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        var user = args.User;

        // limit this to admins
        if (!_admin.IsAdmin(user))
            return;

        // simple verb that just clears the memory list
        var clearMemoryVerb = new Verb()
        {
            Text = Loc.GetString("parrot-verb-clear-memory"),
            Category = VerbCategory.Admin,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/clear-parrot.png")),
            Act = () =>
            {
                _popup.PopupClient(Loc.GetString("parrot-popup-memory-cleared"), entity.Owner, user);

                if (_net.IsServer)
                    entity.Comp.SpeechMemories.Clear();
            },
        };

        args.Verbs.Add(clearMemoryVerb);
    }
}
