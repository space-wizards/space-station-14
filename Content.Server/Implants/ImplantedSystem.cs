using Content.Shared.Implants.Components;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed partial class ImplanterSystem
{
    public void InitializeImplanted()
    {
        SubscribeLocalEvent<ImplantedComponent, ComponentInit>(OnImplantedInit);
    }

    private void OnImplantedInit(EntityUid uid, ImplantedComponent component, ComponentInit args)
    {
        component.ImplantContainer = _container.EnsureContainer<Container>(uid, ImplantSlotId);
        component.ImplantContainer.OccludesLight = false;
    }
}
