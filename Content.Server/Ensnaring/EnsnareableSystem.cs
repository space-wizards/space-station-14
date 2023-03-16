using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Ensnaring;

public sealed partial class EnsnareableSystem : SharedEnsnareableSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeEnsnaring();

        SubscribeLocalEvent<EnsnareableComponent, ComponentInit>(OnEnsnareableInit);
        SubscribeLocalEvent<EnsnareableComponent, EnsnareableDoAfterEvent>(OnDoAfter);
    }

    private void OnEnsnareableInit(EntityUid uid, EnsnareableComponent component, ComponentInit args)
    {
        component.Container = _container.EnsureContainer<Container>(uid, "ensnare");
    }

    private void OnDoAfter(EntityUid uid, EnsnareableComponent component, DoAfterEvent args)
    {
        if (args.Handled || !TryComp<EnsnaringComponent>(args.Args.Used, out var ensnaring))
            return;

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-fail", ("ensnare", args.Args.Used)), uid, uid, PopupType.Large);
            return;
        }

        component.Container.Remove(args.Args.Used.Value);
        component.IsEnsnared = false;
        Dirty(component);
        ensnaring.Ensnared = null;

        _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-complete", ("ensnare", args.Args.Used)), uid, uid, PopupType.Large);

        UpdateAlert(args.Args.Used.Value, component);
        var ev = new EnsnareRemoveEvent();
        RaiseLocalEvent(uid, ev);

        args.Handled = true;
    }
}
