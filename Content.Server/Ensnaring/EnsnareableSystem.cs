using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Ensnaring;

public sealed partial class EnsnareableSystem : SharedEnsnareableSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
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
        if (args.Args.Target == null)
            return;

        if (args.Handled || !TryComp<EnsnaringComponent>(args.Args.Used, out var ensnaring))
            return;

        if (args.Cancelled || !_container.Remove(args.Args.Used.Value, component.Container))
        {
            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-fail", ("ensnare", args.Args.Used)), uid, uid, PopupType.MediumCaution);
            return;
        }

        component.IsEnsnared = component.Container.ContainedEntities.Count > 0;
        Dirty(uid, component);
        ensnaring.Ensnared = null;

        _hands.PickupOrDrop(args.Args.User, args.Args.Used.Value);

        _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-complete", ("ensnare", args.Args.Used)), uid, uid, PopupType.Medium);

        UpdateAlert(args.Args.Target.Value, component);
        var ev = new EnsnareRemoveEvent(ensnaring.WalkSpeed, ensnaring.SprintSpeed);
        RaiseLocalEvent(uid, ev);

        args.Handled = true;
    }
}
