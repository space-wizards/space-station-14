using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

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
        SubscribeLocalEvent<ImplanterComponent, ExaminedEvent>(OnExamine);
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

    private void OnExamine(EntityUid uid, ImplanterComponent component, ExaminedEvent args)
    {
        if (!component.ImplanterSlot.HasItem || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("implanter-contained-implant-text", ("desc", component.ImplantData.Item2)));
    }

    //Instantly implant something and add all necessary components and containers.
    //Set to draw mode if not implant only
    public void Implant(EntityUid user, EntityUid target, EntityUid implanter, ImplanterComponent component)
    {
        if (!CanImplant(user, target, implanter, component, out var implant, out var implantComp))
            return;

        //If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        component.ImplanterSlot.ContainerSlot?.Remove(implant.Value);
        implantComp.ImplantedEntity = target;
        implantContainer.OccludesLight = false;
        implantContainer.Insert(implant.Value);

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            DrawMode(implanter, component);
        else
            ImplantMode(implanter, component);

        Dirty(component);
    }

    public bool CanImplant(
        EntityUid user,
        EntityUid target,
        EntityUid implanter,
        ImplanterComponent component,
        [NotNullWhen(true)] out EntityUid? implant,
        [NotNullWhen(true)] out SubdermalImplantComponent? implantComp)
    {
        implant = component.ImplanterSlot.ContainerSlot?.ContainedEntities.FirstOrDefault();
        if (!TryComp(implant, out implantComp))
            return false;

        var ev = new AddImplantAttemptEvent(user, target, implant.Value, implanter);
        RaiseLocalEvent(target, ev);
        return !ev.Cancelled;
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
                    continue;

                //Don't remove a permanent implant and look for the next that can be drawn
                if (!_container.CanRemove(implant, implantContainer))
                {
                    var implantName = Identity.Entity(implant, EntityManager);
                    var targetName = Identity.Entity(target, EntityManager);
                    var failedPermanentMessage = Loc.GetString("implanter-draw-failed-permanent",
                        ("implant", implantName), ("target", targetName));
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
                ImplantMode(implanter, component);

            Dirty(component);
        }
    }

    private void ImplantMode(EntityUid uid, ImplanterComponent component)
    {
        component.CurrentMode = ImplanterToggleMode.Inject;
        ChangeOnImplantVisualizer(uid, component);
    }

    private void DrawMode(EntityUid uid, ImplanterComponent component)
    {
        component.CurrentMode = ImplanterToggleMode.Draw;
        ChangeOnImplantVisualizer(uid, component);
    }

    private void ChangeOnImplantVisualizer(EntityUid uid, ImplanterComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        bool implantFound;

        if (component.ImplanterSlot.HasItem)
            implantFound = true;

        else
            implantFound = false;

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);

        else if (component.CurrentMode == ImplanterToggleMode.Inject && component.ImplantOnly)
        {
            _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);
            _appearance.SetData(uid, ImplanterImplantOnlyVisuals.ImplantOnly, component.ImplantOnly,
                appearance);
        }

        else
            _appearance.SetData(uid, ImplanterVisuals.Full, implantFound, appearance);
    }
}

[Serializable, NetSerializable]
public sealed partial class ImplantEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class DrawEvent : SimpleDoAfterEvent
{
}

public sealed class AddImplantAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Target;
    public readonly EntityUid Implant;
    public readonly EntityUid Implanter;

    public AddImplantAttemptEvent(EntityUid user, EntityUid target, EntityUid implant, EntityUid implanter)
    {
        User = user;
        Target = target;
        Implant = implant;
        Implanter = implanter;
    }
}
