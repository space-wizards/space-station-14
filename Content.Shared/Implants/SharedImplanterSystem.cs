using System.Linq;
using Content.Shared.Implants.Components;
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

        SubscribeLocalEvent<ImplanterComponent, EntInsertedIntoContainerMessage>(OnEntInserted);

    }

    private void OnEntInserted(EntityUid uid, ImplanterComponent component, EntInsertedIntoContainerMessage args)
    {
        component.NumberOfEntities = args.Container.ContainedEntities.Count;
        var implantData = EntityManager.GetComponent<MetaDataComponent>(args.Entity);
        component.ImplantData = (implantData.EntityName, implantData.EntityDescription);
    }

    private void OnStartup(EntityUid uid, ImplanterComponent component, ComponentStartup args)
    {

    }

    //Instantly implant something and add all necessary components and containers.
    //Set to draw mode if not implant only
    public void Implant(EntityUid implanter, EntityUid target, ImplanterComponent component)
    {
        if (!_container.TryGetContainer(implanter, ImplanterSlotId, out var implanterContainer))
            return;

        var implant = implanterContainer.ContainedEntities.FirstOrDefault();

        if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
            return;

        //If the target doesn't have the implanted component, add it.
        if (!HasComp<ImplantedComponent>(target))
            EnsureComp<ImplantedComponent>(target);

        var implantContainer = _container.EnsureContainer<Container>(target, ImplantSlotId);
        implantComp.EntityUid = target;
        implanterContainer.Remove(implant);
        component.NumberOfEntities = implanterContainer.ContainedEntities.Count;
        implantContainer.OccludesLight = false;
        implantContainer.Insert(implant);


        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            DrawMode(component);

        else
            ImplantMode(component);

        Dirty(component);
    }

    //Draw the implant out of the target
    //TODO: Completely remove when surgery is in
    public void Draw(EntityUid implanter, EntityUid target, ImplanterComponent component)
    {
        if (!_container.TryGetContainer(implanter, ImplanterSlotId, out var implanterContainer))
            return;

        if (_container.TryGetContainer(target, ImplantSlotId, out var implantContainer))
        {
            var implant = implantContainer.ContainedEntities.FirstOrDefault();
            if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
                return;

            implantContainer.Remove(implant);
            implantComp.EntityUid = null;
            implanterContainer.Insert(implant);
            component.NumberOfEntities = implanterContainer.ContainedEntities.Count;

            if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly)
                ImplantMode(component);
        }
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

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, component.NumberOfEntities, appearance);

        else if (component.CurrentMode == ImplanterToggleMode.Inject && component.ImplantOnly)
        {
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, component.NumberOfEntities, appearance);
            _appearance.SetData(component.Owner, ImplanterImplantOnlyVisuals.ImplantOnly, component.ImplantOnly, appearance);
        }

        else
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, component.NumberOfEntities, appearance);
    }
}
