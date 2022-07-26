using Content.Server.Ensnaring.Components;
using Content.Server.Popups;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Ensnaring;

public sealed class EnsnareableSystem : SharedEnsnareableSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EnsnaringSystem _ensnaring = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableComponent, ComponentInit>(OnEnsnareableInit);
        SubscribeLocalEvent<EnsnareableComponent, FreeEnsnareDoAfterComplete>(OnFreeComplete);
        SubscribeLocalEvent<EnsnareableComponent, FreeEnsnareDoAfterCancel>(OnFreeFail);
    }

    private void OnEnsnareableInit(EntityUid uid, EnsnareableComponent component, ComponentInit args)
    {
        component.Container = _container.EnsureContainer<Container>(component.Owner, "Ensnare Container");
    }

    private void OnFreeComplete(EntityUid uid, EnsnareableComponent component, FreeEnsnareDoAfterComplete args)
    {
        if (args.EnsnaringComponent != null)
        {
            component.Container.ForceRemove(args.EnsnaringComponent.Owner);
            component.IsEnsnared = false;
            args.EnsnaringComponent.Ensnared = null;

            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-complete", ("ensnare", args.EnsnaringComponent.Owner)),
                uid, Filter.Entities(uid), PopupType.Large);

            _ensnaring.UpdateAlert(component);
            var ev = new EnsnareRemoveEvent();
            RaiseLocalEvent(uid, ev, false);

            args.EnsnaringComponent.CancelToken = null;
        }
    }

    private void OnFreeFail(EntityUid uid, EnsnareableComponent component, FreeEnsnareDoAfterCancel args)
    {
        if (args.EnsnaringComponent != null)
        {
            args.EnsnaringComponent.CancelToken = null;

            _popup.PopupEntity(Loc.GetString("ensnare-component-try-free-fail", ("ensnare", args.EnsnaringComponent.Owner)),
                uid, Filter.Entities(uid), PopupType.Large);
        }
    }
}
