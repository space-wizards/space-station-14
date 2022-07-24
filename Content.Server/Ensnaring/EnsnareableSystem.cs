using Content.Server.Ensnaring.Components;
using Content.Shared.Alert;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Ensnaring;

public sealed class EnsnareableSystem : SharedEnsnareableSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableComponent, ComponentInit>(OnEnsnareableInit);
    }

    private void OnEnsnareableInit(EntityUid uid, EnsnareableComponent component, ComponentInit args)
    {
        component.Container = _container.EnsureContainer<Container>(component.Owner, "Ensnare Container");
    }

    /// <summary>
    /// Used where you want to try to free an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="ensnaringEntity">The entity that was used to ensnare</param>
    /// <param name="target">The entity that will be free</param>
    /// <param name="component">The ensnaring component</param>
    public void TryFree(EntityUid ensnaringEntity, EntityUid target, EnsnaringComponent component)
    {
        //Don't do anything if they don't have the ensnareable component.
        if (!TryComp<EnsnareableComponent>(target, out var ensnareable))
            return;

        ensnareable.EnsnaringEntity = null;
        ensnareable.Container.ForceRemove(ensnaringEntity);
        ensnareable.IsEnsnared = false;

        UpdateAlert(ensnareable);
        var ev = new EnsnareChangeEvent(ensnaringEntity, component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(target, ev, false);
    }

    public void UpdateAlert(EnsnareableComponent component)
    {
        if (!component.IsEnsnared)
        {
            _alerts.ClearAlert(component.Owner, AlertType.Ensnared);
        }
        else
        {
            _alerts.ShowAlert(component.Owner, AlertType.Ensnared);
        }
    }
}
