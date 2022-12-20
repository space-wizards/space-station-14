using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.Implants;

public abstract class SharedImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, ComponentInit>(OnImplanterInit);
        SubscribeLocalEvent<ImplanterComponent, EntInsertedIntoContainerMessage>(OnEntInserted);

    }

    private void OnImplanterInit(EntityUid uid, ImplanterComponent component, ComponentInit args)
    {
        if (component.Implant != null)
            component.ImplanterSlot.StartingItem = component.Implant;

        _itemSlots.AddItemSlot(uid, ImplanterComponent.ImplanterSlotId, component.ImplanterSlot);
    }

    private void OnEntInserted(EntityUid uid, ImplanterComponent component, EntInsertedIntoContainerMessage args)
    {
        var implantData = EntityManager.GetComponent<MetaDataComponent>(args.Entity);
        component.ImplantData = (implantData.EntityName, implantData.EntityDescription);
    }

    //Instantly implant something and add all necessary components and containers.
    //Set to draw mode if not implant only
    public void Implant(EntityUid implanter, EntityUid target, ImplanterComponent component)
    {
        var implanterContainer = component.ImplanterSlot.ContainerSlot;

        if (implanterContainer is null)
            return;

        var implant = implanterContainer.ContainedEntities.FirstOrDefault();

        if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
            return;

        //If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        implanterContainer.Remove(implant);
        implantComp.ImplantedEntity = target;
        implantContainer.OccludesLight = false;
        implantContainer.Insert(implant);

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            DrawMode(component);

        else
            ImplantMode(component);

        Dirty(component);
    }

    //Draw the implant out of the target
    //TODO: Rework when surgery is in so implant cases can be a thing
    public void Draw(EntityUid implanter, EntityUid user, EntityUid target, ImplanterComponent component)
    {
        var implanterContainer = component.ImplanterSlot.ContainerSlot;

        if (implanterContainer is null)
            return;

        var permanentFound = false;

        if (_container.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer))
        {
            var implantCompQuery = GetEntityQuery<SubdermalImplantComponent>();

            foreach (var implant in implantContainer.ContainedEntities)
            {
                if (!implantCompQuery.TryGetComponent(implant, out var implantComp))
                    return;

                //Don't remove a permanent implant and look for the next that can be drawn
                if (!implantContainer.CanRemove(implant))
                {
                    var implantName = Identity.Entity(implant, EntityManager);
                    var targetName = Identity.Entity(target, EntityManager);
                    var failedPermanentMessage = Loc.GetString("implanter-draw-failed-permanent", ("implant", implantName), ("target", targetName));
                    _popup.PopupEntity(failedPermanentMessage, target, user);
                    permanentFound = implantComp.Permanent;
                    continue;
                }

                implantContainer.Remove(implant);
                implantComp.ImplantedEntity = null;
                implanterContainer.Insert(implant);
                permanentFound = implantComp.Permanent;
                //Break so only one implant is drawn
                break;
            }

            if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly && !permanentFound)
                ImplantMode(component);

            Dirty(component);
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

        bool implantFound;

        if (component.ImplanterSlot.HasItem)
            implantFound = true;

        else
            implantFound = false;

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, implantFound, appearance);

        else if (component.CurrentMode == ImplanterToggleMode.Inject && component.ImplantOnly)
        {
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, implantFound, appearance);
            _appearance.SetData(component.Owner, ImplanterImplantOnlyVisuals.ImplantOnly, component.ImplantOnly, appearance);
        }

        else
            _appearance.SetData(component.Owner, ImplanterVisuals.Full, implantFound, appearance);
    }
}
