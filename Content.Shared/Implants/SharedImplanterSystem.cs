using System.Linq;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared.Implants;

public abstract class SharedImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public const string ImplanterSlotId = "implanter_slot";
    public const string ImplantSlotId = "implantcontainer";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, ComponentInit>(OnImplanterInit);
    }

    private void OnImplanterInit(EntityUid uid, ImplanterComponent component, ComponentInit args)
    {
        var implanterContainer = _container.GetContainer(component.Owner, ImplanterSlotId);

        if (implanterContainer.ContainedEntities.Count >= 1)
        {
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, true);
        }
    }

    //Instantly implant something and add all necessary components and containers.
    //Set to draw mode if not implant only
    public void Implant(EntityUid implanter, EntityUid target, ImplanterComponent component)
    {
        if (!_container.TryGetContainer(implanter, ImplanterSlotId, out var container))
            return;

        var implant = container.ContainedEntities.FirstOrDefault();

        if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
            return;

        //If the target doesn't have the implanted component, add it.
        if (!HasComp<ImplantedComponent>(target))
            EnsureComp<ImplantedComponent>(target);

        var implantContainer = _container.EnsureContainer<Container>(target, ImplantSlotId);
        implantComp.EntityUid = target;
        container.Remove(implant);
        implantContainer.OccludesLight = false;
        implantContainer.Insert(implant);

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            DrawMode(component);

        Dirty(component);
    }

    private void ImplantMode(ImplanterComponent component)
    {
        component.CurrentMode = ImplanterToggleMode.Inject;
        ChangeOnImplantVisualizer(component);
    }

    private void DrawMode(ImplanterComponent component)
    {
        component.CurrentMode = ImplanterToggleMode.Draw;
        ChangeOnImplantVisualizer(component);
    }

    private void ChangeOnImplantVisualizer(ImplanterComponent component)
    {
        if (!TryComp<AppearanceComponent>(component.Owner, out var appearance))
            return;

        if (component.CurrentMode == ImplanterToggleMode.Inject)
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, true, appearance);

        else
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, false, appearance);
    }
}
