using Content.Server.Popups;
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
        SubscribeLocalEvent<EnsnareableComponent, FreeEnsnareDoAfterComplete>(OnFreeComplete);
        SubscribeLocalEvent<EnsnareableComponent, FreeEnsnareDoAfterCancel>(OnFreeFail);
    }

    private void OnEnsnareableInit(EntityUid uid, EnsnareableComponent component, ComponentInit args)
    {
        component.Container = _container.EnsureContainer<Container>(component.Owner, "ensnare");
    }

    private void OnFreeComplete(EntityUid uid, EnsnareableComponent component, FreeEnsnareDoAfterComplete args)
    {
        if (!TryComp<EnsnaringComponent>(args.EnsnaringEntity, out var ensnaring))
            return;

        component.Container.Remove(args.EnsnaringEntity);
        component.IsEnsnared = false;
        Dirty(component);
        ensnaring.Ensnared = null;

        _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-complete", ("ensnare", args.EnsnaringEntity)),
            uid, uid, PopupType.Large);

        UpdateAlert(component);
        var ev = new EnsnareRemoveEvent();
        RaiseLocalEvent(uid, ev);

        ensnaring.CancelToken = null;
    }

    private void OnFreeFail(EntityUid uid, EnsnareableComponent component, FreeEnsnareDoAfterCancel args)
    {
        if (!TryComp<EnsnaringComponent>(args.EnsnaringEntity, out var ensnaring))
            return;

        ensnaring.CancelToken = null;

        _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-fail", ("ensnare", args.EnsnaringEntity)),
            uid, uid, PopupType.Large);
    }
}
