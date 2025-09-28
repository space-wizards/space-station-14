using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
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

        // STARLIGHT START: Check if the implant is a USSP uplink implant (revolutionary implant)
        var isUSSPImplant = false;
        if (TryComp<MetaDataComponent>(implant.Value, out var metadata) && 
            metadata.EntityPrototype?.ID == "USSPUplinkImplant")
        {
            isUSSPImplant = true;
        }

        // If this is a USSP uplink implant, perform revolutionary-specific checks
        if (isUSSPImplant)
        {
            // Check if the target is a revolutionary or head revolutionary
            var targetIsRev = HasComp<RevolutionaryComponent>(target);
            var targetIsHeadRev = HasComp<HeadRevolutionaryComponent>(target);
            
            // Check if the user is a head revolutionary
            var userIsHeadRev = HasComp<HeadRevolutionaryComponent>(user);
            var userIsRev = HasComp<RevolutionaryComponent>(user);
            
            // If the user is not a revolutionary or head revolutionary, they can't use the implant
            if (!userIsHeadRev && !userIsRev)
            {
                _popup.PopupEntity(Loc.GetString("Useless junk."), user, user);
                return false;
            }
            
            // Check if the target already has a USSP uplink implant
            if (TryComp<ImplantedComponent>(target, out var implanted) && implanted.ImplantContainer != null)
            {
                foreach (var existingImplant in implanted.ImplantContainer.ContainedEntities)
                {
                    if (TryComp<MetaDataComponent>(existingImplant, out var existingMetadata) && 
                        existingMetadata.EntityPrototype?.ID == "USSPUplinkImplant")
                    {
                        _popup.PopupEntity(Loc.GetString("Already has an uplink implant."), user, user);
                        return false;
                    }
                }
            }
            
            // If the target is a head revolutionary, prevent implantation unless it's self-implantation
            // Only show "Can't implant another headrev!" if the user is a revolutionary or head revolutionary
            if (targetIsHeadRev && target != user)
            {
                if (userIsHeadRev || userIsRev)
                {
                    _popup.PopupEntity(Loc.GetString("Can't implant another headrev!"), user, user);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("Useless junk."), user, user);
                }
                return false;
            }
            
            // Check if the implant has an owner component
            var hasOwner = TryComp<USSPUplinkOwnerComponent>(implant.Value, out var ownerComp);
            
            // If the user is a head revolutionary
            if (userIsHeadRev)
            {
                // If the target is not a revolutionary and not a head revolutionary (or self), prevent implantation
                if (!targetIsRev && !(targetIsHeadRev && target == user))
                {
                    _popup.PopupEntity(Loc.GetString("Not converted by you."), user, user);
                    return false;
                }
                
                // If the target is a revolutionary, check if they were converted by a different head revolutionary
                if (targetIsRev)
                {
                    // First check if the target has a RevolutionaryConverterComponent
                    if (TryComp<RevolutionaryConverterComponent>(target, out var converterComp) && 
                        converterComp.ConverterUid != null && 
                        converterComp.ConverterUid != user)
                    {
                        // If the target was converted by a different head revolutionary, prevent implantation
                        _popup.PopupEntity(Loc.GetString("Not converted by you."), user, user);
                        return false;
                    }
                    
                    // If the target doesn't have a RevolutionaryConverterComponent or it's not set,
                    // fall back to checking the implant owner
                    if (hasOwner && ownerComp != null && ownerComp.OwnerUid != null)
                    {
                        // If the implant has an owner and it's not the current user, this revolutionary
                        // was likely converted by a different head revolutionary
                        if (ownerComp.OwnerUid != user)
                        {
                            _popup.PopupEntity(Loc.GetString("Not converted by you."), user, user);
                            return false;
                        }
                    }
                    else
                    {
                        // If the implant doesn't have an owner yet, we need to check if the target
                        // already has an implant with an owner that's not the current user
                        if (TryComp<ImplantedComponent>(target, out var targetImplanted) && targetImplanted.ImplantContainer != null)
                        {
                            foreach (var existingImplant in targetImplanted.ImplantContainer.ContainedEntities)
                            {
                                if (TryComp<USSPUplinkOwnerComponent>(existingImplant, out var existingOwnerComp) && 
                                    existingOwnerComp.OwnerUid != null && 
                                    existingOwnerComp.OwnerUid != user)
                                {
                                    // If the target has an implant owned by a different head revolutionary,
                                    // they were likely converted by that head revolutionary
                                    _popup.PopupEntity(Loc.GetString("Not converted by you."), user, user);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            // If the user is a revolutionary (not head)
            else if (userIsRev)
            {
                // If the user is trying to implant themselves with an implant from a different head revolutionary
                if (user == target && userIsRev && !userIsHeadRev)
                {
                    // First check if the user has a RevolutionaryConverterComponent
                    if (TryComp<RevolutionaryConverterComponent>(user, out var converterComp) && 
                        converterComp.ConverterUid != null)
                    {
                        // If the implant has an owner component
                        if (hasOwner && ownerComp != null && ownerComp.OwnerUid != null)
                        {
                            // If the implant's owner is not the user's converter, prevent implantation
                            if (ownerComp.OwnerUid != converterComp.ConverterUid)
                            {
                                _popup.PopupEntity(Loc.GetString("Not your headrev."), user, user);
                                return false;
                            }
                        }
                        else
                        {
                            // If the implant doesn't have an owner yet, we need to check if the user
                            // already has an implant with an owner that matches their converter
                            bool hasMatchingImplant = false;
                            
                            if (TryComp<ImplantedComponent>(user, out var userImplanted) && userImplanted.ImplantContainer != null)
                            {
                                foreach (var existingImplant in userImplanted.ImplantContainer.ContainedEntities)
                                {
                                    if (TryComp<USSPUplinkOwnerComponent>(existingImplant, out var existingOwnerComp) && 
                                        existingOwnerComp.OwnerUid != null && 
                                        existingOwnerComp.OwnerUid == converterComp.ConverterUid)
                                    {
                                        // Found an implant owned by the user's converter
                                        hasMatchingImplant = true;
                                        break;
                                    }
                                }
                            }
                            
                            // If no matching implant was found, prevent implantation
                            if (!hasMatchingImplant)
                            {
                                _popup.PopupEntity(Loc.GetString("Not your headrev."), user, user);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // If the user doesn't have a RevolutionaryConverterComponent, 
                        // they shouldn't be able to implant themselves with any USSP uplink
                        _popup.PopupEntity(Loc.GetString("Not your headrev."), user, user);
                        return false;
                    }
                }
                // If the user is trying to implant someone else
                else if (user != target)
                {
                    _popup.PopupEntity(Loc.GetString("Useless junk."), user, user);
                    return false;
                }
            }
        }
        // STARLIGHT END

        var ev = new AddImplantAttemptEvent(user, target, implant.Value, implanter);
        RaiseLocalEvent(target, ev);
        return !ev.Cancelled;
    }

    protected bool CheckTarget(EntityUid target, EntityWhitelist? whitelist, EntityWhitelist? blacklist)
    {
        return _whitelistSystem.IsWhitelistPassOrNull(whitelist, target) &&
            _whitelistSystem.IsBlacklistFailOrNull(blacklist, target);
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
