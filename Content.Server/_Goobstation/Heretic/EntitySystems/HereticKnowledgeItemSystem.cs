using Content.Server.Heretic.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Heretic;
using Content.Shared.Interaction.Events;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticKnowledgeItemSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticKnowledgeItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HereticKnowledgeItemComponent, HereticKnowledgeItemDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HereticKnowledgeItemComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUseInHand(Entity<HereticKnowledgeItemComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !HasComp<HereticComponent>(args.User))
            return;

        var user = args.User;
        if (ent.Comp.Spent)
            return;

        var dargs = new DoAfterArgs(EntityManager, args.User, 10f, new HereticKnowledgeItemDoAfterEvent(), ent, used: ent);
        _popup.PopupEntity(Loc.GetString("heretic-item-start"), ent, user);
        _doafter.TryStartDoAfter(dargs);
        args.Handled = true;
    }

    private void OnDoAfter(Entity<HereticKnowledgeItemComponent> ent, ref HereticKnowledgeItemDoAfterEvent args)
    {
        if (args.Cancelled || !TryComp<HereticComponent>(args.User, out var heretic))
        {
            return;
        }
        _heretic.UpdateKnowledge(args.User, heretic, 1f);

        ent.Comp.Spent = true;
    }

    private void OnExamined(Entity<HereticKnowledgeItemComponent> ent, ref ExaminedEvent args)
    {
        if(TryComp<HereticComponent>(args.Examiner, out _))
        {
            if (ent.Comp.Spent)
            {
                args.PushMarkup(markup: $"[color=purple]{Loc.GetString("heretic-item-examine-spent")}[/color]");
            }
            else
            {
                args.PushMarkup(markup: $"[color=purple]{Loc.GetString("heretic-item-examine-unspent")}[/color]");
            }
        }
        else
        {
            args.PushMarkup(markup: $"[color=purple]{Loc.GetString("heretic-item-examine-nonheretic")}[/color]");
        }
    }
}
