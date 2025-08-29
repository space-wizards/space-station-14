using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

public abstract partial class SharedHumanoidAppearanceSystem
{
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    private const string BuiType = "HumanoidMarkingModifierBoundUserInterface";

    private void OnVerbsRequest(Entity<HumanoidAppearanceComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor)
            || !_adminManager.HasAdminFlag(actor.PlayerSession, AdminFlags.Fun))
            return;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("marking-modifier-verb"),
            Category = VerbCategory.Tricks,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Customization/reptilian_parts.rsi"), "tail_smooth"),
            Act = () =>
            {
                // TODO this is... definitely wrong right?
                // I just want no range check aaa
                UI.SetUi(ent.Owner, HumanoidMarkingModifierKey.Key, new(BuiType, -1));
                UI.OpenUi(ent.Owner, HumanoidMarkingModifierKey.Key, actor.PlayerSession);
            },
        });
    }

    private void OnBaseLayersSet(Entity<HumanoidAppearanceComponent> ent,
        ref HumanoidMarkingModifierBaseLayersSetMessage message)
    {
        if (!_adminManager.HasAdminFlag(message.Actor, AdminFlags.Fun))
            return;

        if (message.Info is { } info)
            ent.Comp.CustomBaseLayers[message.Layer] = info;
        else
            ent.Comp.CustomBaseLayers.Remove(message.Layer);

        Dirty(ent);
    }

    private void OnMarkingsSet(Entity<HumanoidAppearanceComponent> ent,
        ref HumanoidMarkingModifierMarkingSetMessage message)
    {
        if (!_adminManager.HasAdminFlag(message.Actor, AdminFlags.Fun))
            return;

        ent.Comp.MarkingSet = message.MarkingSet;
        Dirty(ent);
    }
}
