using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Implants;

public abstract class SharedImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, ComponentInit>(OnImplanterInit);
        SubscribeLocalEvent<ImplanterComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ImplanterComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ImplanterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ImplanterComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
        SubscribeLocalEvent<ImplanterComponent, DeimplantChangeVerbMessage>(OnSelected);
    }

    private void OnImplanterInit(EntityUid uid, ImplanterComponent component, ComponentInit args)
    {
        if (component.Implant != null)
            component.ImplanterSlot.StartingItem = component.Implant;

        _itemSlots.AddItemSlot(uid, ImplanterComponent.ImplanterSlotId, component.ImplanterSlot);

        component.DeimplantChosen ??= component.DeimplantWhitelist.FirstOrNull();

        Dirty(uid, component);
    }

    private void OnEntInserted(EntityUid uid, ImplanterComponent component, EntInsertedIntoContainerMessage args)
    {
        var implantData = Comp<MetaDataComponent>(args.Entity);
        component.ImplantData = (implantData.EntityName, implantData.EntityDescription);
    }

    private void OnExamine(EntityUid uid, ImplanterComponent component, ExaminedEvent args)
    {
        if (!component.ImplanterSlot.HasItem || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("implanter-contained-implant-text", ("desc", component.ImplantData.Item2)));
    }
    public bool CheckSameImplant(EntityUid target, EntityUid implant)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return false;
        var implantPrototype = Prototype(implant);
        return implanted.ImplantContainer.ContainedEntities.Any(entity => Prototype(entity) == implantPrototype);
    }

    private void OnVerb(EntityUid uid, ImplanterComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (component.CurrentMode == ImplanterToggleMode.Draw)
        {
            args.Verbs.Add(new InteractionVerb()
            {
                Text = Loc.GetString("implanter-set-draw-verb"),
                Act = () => TryOpenUi(uid, args.User, component)
            });
        }
    }

    private void OnUseInHand(EntityUid uid, ImplanterComponent? component, UseInHandEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentMode == ImplanterToggleMode.Draw)
            TryOpenUi(uid, args.User, component);
    }

    private void OnSelected(EntityUid uid, ImplanterComponent component, DeimplantChangeVerbMessage args)
    {
        component.DeimplantChosen = args.Implant;
        SetSelectedDeimplant(uid, args.Implant, component: component);
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, ImplanterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        _uiSystem.TryToggleUi(uid, DeimplantUiKey.Key, user);
        component.DeimplantChosen ??= component.DeimplantWhitelist.FirstOrNull();
        Dirty(uid, component);
    }

    //Instantly implant something and add all necessary components and containers.
    //Set to draw mode if not implant only
    public void Implant(EntityUid user, EntityUid target, EntityUid implanter, ImplanterComponent component)
    {
        if (!CanImplant(user, target, implanter, component, out var implant, out _))
            return;

        // Check if we are trying to implant a implant which is already implanted
        // Check AFTER the doafter to prevent "is it a fake?" metagaming against deceptive implants
        if (!component.AllowMultipleImplants && CheckSameImplant(target, implant.Value))
        {
            var name = Identity.Name(target, EntityManager, user);
            var msg = Loc.GetString("implanter-component-implant-already", ("implant", implant), ("target", name));
            _popup.PopupEntity(msg, target, user);
            return;
        }

        //If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        if (component.ImplanterSlot.ContainerSlot != null)
            _container.Remove(implant.Value, component.ImplanterSlot.ContainerSlot);
        implantContainer.OccludesLight = false;
        _container.Insert(implant.Value, implantContainer);

        if (component.CurrentMode == ImplanterToggleMode.Inject && !component.ImplantOnly)
            DrawMode(implanter, component);
        else
            ImplantMode(implanter, component);

        var ev = new TransferDnaEvent { Donor = target, Recipient = implanter };
        RaiseLocalEvent(target, ref ev);

        Dirty(implanter, component);
    }

    public bool CanImplant(
        EntityUid user,
        EntityUid target,
        EntityUid implanter,
        ImplanterComponent component,
        [NotNullWhen(true)] out EntityUid? implant,
        [NotNullWhen(true)] out SubdermalImplantComponent? implantComp)
    {
        implant = component.ImplanterSlot.ContainerSlot?.ContainedEntities.FirstOrNull();
        if (!TryComp(implant, out implantComp))
            return false;

        if (!CheckTarget(target, component.Whitelist, component.Blacklist) ||
            !CheckTarget(target, implantComp.Whitelist, implantComp.Blacklist))
        {
            return false;
        }

        var ev = new AddImplantAttemptEvent(user, target, implant.Value, implanter);
        RaiseLocalEvent(target, ev);
        return !ev.Cancelled;
    }

    protected bool CheckTarget(EntityUid target, EntityWhitelist? whitelist, EntityWhitelist? blacklist)
    {
        return _whitelistSystem.IsWhitelistPassOrNull(whitelist, target) &&
            _whitelistSystem.IsWhitelistFailOrNull(blacklist, target);
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

            if (component.AllowDeimplantAll)
            {
                foreach (var implant in implantContainer.ContainedEntities)
                {
                    if (!implantCompQuery.TryGetComponent(implant, out var implantComp))
                        continue;

                    //Don't remove a permanent implant and look for the next that can be drawn
                    if (!_container.CanRemove(implant, implantContainer))
                    {
                        DrawPermanentFailurePopup(implant, target, user);
                        permanentFound = implantComp.Permanent;
                        continue;
                    }

                    DrawImplantIntoImplanter(implanter, target, implant, implantContainer, implanterContainer, implantComp);
                    permanentFound = implantComp.Permanent;

                    //Break so only one implant is drawn
                    break;
                }

                if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly && !permanentFound)
                    ImplantMode(implanter, component);
            }
            else
            {
                EntityUid? implant = null;
                var implants = implantContainer.ContainedEntities;
                foreach (var implantEntity in implants)
                {
                    if (TryComp<SubdermalImplantComponent>(implantEntity, out var subdermalComp))
                    {
                        if (component.DeimplantChosen == subdermalComp.DrawableProtoIdOverride ||
                            (Prototype(implantEntity) != null && component.DeimplantChosen == Prototype(implantEntity)!))
                            implant = implantEntity;
                    }
                }

                if (implant != null && implantCompQuery.TryGetComponent(implant, out var implantComp))
                {
                    //Don't remove a permanent implant
                    if (!_container.CanRemove(implant.Value, implantContainer))
                    {
                        DrawPermanentFailurePopup(implant.Value, target, user);
                        permanentFound = implantComp.Permanent;

                    }
                    else
                    {
                        DrawImplantIntoImplanter(implanter, target, implant.Value, implantContainer, implanterContainer, implantComp);
                        permanentFound = implantComp.Permanent;
                    }

                    if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly && !permanentFound)
                        ImplantMode(implanter, component);
                }
                else
                {
                    DrawCatastrophicFailure(implanter, component, user);
                }
            }

            Dirty(implanter, component);

        }
        else
        {
            DrawCatastrophicFailure(implanter, component, user);
        }
    }

    private void DrawPermanentFailurePopup(EntityUid implant, EntityUid target, EntityUid user)
    {
        var implantName = Identity.Entity(implant, EntityManager);
        var targetName = Identity.Entity(target, EntityManager);
        var failedPermanentMessage = Loc.GetString("implanter-draw-failed-permanent",
            ("implant", implantName), ("target", targetName));
        _popup.PopupEntity(failedPermanentMessage, target, user);
    }

    private void DrawImplantIntoImplanter(EntityUid implanter, EntityUid target, EntityUid implant, BaseContainer implantContainer, ContainerSlot implanterContainer, SubdermalImplantComponent implantComp)
    {
        _container.Remove(implant, implantContainer);
        _container.Insert(implant, implanterContainer);

        var ev = new TransferDnaEvent { Donor = target, Recipient = implanter };
        RaiseLocalEvent(target, ref ev);
    }

    private void DrawCatastrophicFailure(EntityUid implanter, ImplanterComponent component, EntityUid user)
    {
        _damageableSystem.TryChangeDamage(user, component.DeimplantFailureDamage, ignoreResistances: true, origin: implanter);
        var userName = Identity.Entity(user, EntityManager);
        var failedCatastrophicallyMessage = Loc.GetString("implanter-draw-failed-catastrophically", ("user", userName));
        _popup.PopupEntity(failedCatastrophicallyMessage, user, PopupType.MediumCaution);
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

    public void SetSelectedDeimplant(EntityUid uid, string? implant, ImplanterComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (implant != null && _proto.TryIndex(implant, out EntityPrototype? proto))
            component.DeimplantChosen = proto;

        Dirty(uid, component);
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

/// <summary>
/// Change the chosen implanter in the UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class DeimplantChangeVerbMessage : BoundUserInterfaceMessage
{
    public readonly string? Implant;

    public DeimplantChangeVerbMessage(string? implant)
    {
        Implant = implant;
    }
}

[Serializable, NetSerializable]
public enum DeimplantUiKey : byte
{
    Key
}
