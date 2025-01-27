using Content.Server.Heretic.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Heretic;
using Content.Shared.Interaction;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class EldritchInfluenceSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EldritchInfluenceComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<EldritchInfluenceComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EldritchInfluenceComponent, EldritchInfluenceDoAfterEvent>(OnDoAfter);
    }

    public bool CollectInfluence(Entity<EldritchInfluenceComponent> influence, Entity<HereticComponent> user, EntityUid? used = null)
    {
        if (influence.Comp.Spent)
            return false;

        var ev = new CheckMagicItemEvent();
        RaiseLocalEvent(user, ev);
        if (used != null) RaiseLocalEvent((EntityUid) used, ev);

        var doAfter = new EldritchInfluenceDoAfterEvent()
        {
            MagicItemActive = ev.Handled,
        };
        var dargs = new DoAfterArgs(EntityManager, user, 10f, doAfter, influence, influence);
        _popup.PopupEntity(Loc.GetString("heretic-influence-start"), influence, user);
        return _doafter.TryStartDoAfter(dargs);
    }

    private void OnInteract(Entity<EldritchInfluenceComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled
        || !TryComp<HereticComponent>(args.User, out var heretic))
            return;

        args.Handled = CollectInfluence(ent, (args.User, heretic));
    }
    private void OnInteractUsing(Entity<EldritchInfluenceComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
        || !TryComp<HereticComponent>(args.User, out var heretic))
            return;

        args.Handled = CollectInfluence(ent, (args.User, heretic), args.Used);
    }
    private void OnDoAfter(Entity<EldritchInfluenceComponent> ent, ref EldritchInfluenceDoAfterEvent args)
    {
        if (args.Cancelled
        || args.Target == null
        || !TryComp<HereticComponent>(args.User, out var heretic))
            return;

        _heretic.UpdateKnowledge(args.User, heretic, args.MagicItemActive ? 2 : 1);

        Spawn("EldritchInfluenceIntermediate", Transform((EntityUid) args.Target).Coordinates);
        QueueDel(args.Target);
    }
}
