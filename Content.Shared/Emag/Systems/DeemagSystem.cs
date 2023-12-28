using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared.Emag.Systems;

public sealed class DeemagSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeemagComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DeemagComponent, DeemagDoAfterEvent>(OnDeemag);
    }

    private void OnAfterInteract(Entity<DeemagComponent> deemag, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target || !HasComp<EmaggedComponent>(args.Target))
            return;

        args.Handled = true;
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, deemag.Comp.Duration, new DeemagDoAfterEvent(), deemag, target: args.Target, used: deemag)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true
        });
    }

    private void OnDeemag(Entity<DeemagComponent> deemag, ref DeemagDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target)
            return;

        _popup.PopupClient(Loc.GetString("deemag-success", ("target", Identity.Entity(target, EntityManager))), args.User,
            args.User, PopupType.Medium);

        RemComp<EmaggedComponent>(target);

        var ev = new GotDeemaggedEvent(args.User);
        RaiseLocalEvent(target, ref ev);

        _adminLogger.Add(LogType.Emag, LogImpact.Medium, $"{ToPrettyString(args.User):player} deemagged {ToPrettyString(target):target}");

        if (deemag.Comp.Consumable)
            QueueDel(deemag);
    }
}

[ByRefEvent]
public record struct GotDeemaggedEvent(EntityUid User);
