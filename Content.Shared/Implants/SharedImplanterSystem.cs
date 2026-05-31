using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Implants;

public abstract partial class SharedImplanterSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [Dependency] private EntityQuery<SubdermalImplantComponent> _implantCompQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeImplanted();

        SubscribeLocalEvent<ImplanterComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ImplanterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ImplanterComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ImplanterComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ImplanterComponent, AfterInteractEvent>(OnImplanterAfterInteract);
        SubscribeLocalEvent<ImplanterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ImplanterComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
        SubscribeLocalEvent<ImplanterComponent, DeimplantChangeVerbMessage>(OnSelected);

        SubscribeLocalEvent<ImplanterComponent, ImplantEvent>(OnImplant);
        SubscribeLocalEvent<ImplanterComponent, DrawEvent>(OnDraw);
    }

    private void OnComponentInit(Entity<ImplanterComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.Implant != null)
            ent.Comp.ImplanterSlot.StartingItem = ent.Comp.Implant;

        _itemSlots.AddItemSlot(ent, ImplanterComponent.ImplanterSlotId, ent.Comp.ImplanterSlot);
    }

    private void OnMapInit(Entity<ImplanterComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.DeimplantChosen ??= ent.Comp.DeimplantWhitelist.FirstOrNull();
        Dirty(ent);
    }

    private void OnEntInserted(Entity<ImplanterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return; // Already networked with the same game state.

        if (args.Container.ID != ImplanterComponent.ImplanterSlotId)
            return;

        var implantData = MetaData(args.Entity);
        ent.Comp.ImplantData = (implantData.EntityName, implantData.EntityDescription);
        Dirty(ent);
    }

    private void OnExamine(Entity<ImplanterComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ImplanterSlot.HasItem || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("implanter-contained-implant-text", ("desc", ent.Comp.ImplantData.Item2)));
    }

    private void OnImplanterAfterInteract(Entity<ImplanterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || args.Handled)
            return;

        var target = args.Target.Value;
        if (!CheckTarget(target, ent.Comp.Whitelist, ent.Comp.Blacklist))
            return;

        // TODO: Rework drawing to work with implant cases when surgery is in
        if (ent.Comp.CurrentMode == ImplanterToggleMode.Draw && !ent.Comp.ImplantOnly)
        {
            TryDraw(ent, args.User, target);
        }
        else
        {
            if (!CanImplant(args.User, target, ent, out var implant, out _))
            {
                // no popup if implant doesn't exist
                if (implant == null)
                    return;

                // show popup to the user saying implant failed
                var name = Identity.Name(target, EntityManager, args.User);
                var msg = Loc.GetString("implanter-component-implant-failed", ("implant", implant), ("target", name));
                _popup.PopupClient(msg, target, args.User);
                // prevent further interaction since popup was shown
                args.Handled = true;
                return;
            }

            // Implant self instantly, otherwise try to inject the target.
            if (args.User == target)
                Implant(target, target, ent);
            else
                TryImplant(ent, args.User, target);
        }

        args.Handled = true;
    }

    private void OnVerb(Entity<ImplanterComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (ent.Comp.CurrentMode == ImplanterToggleMode.Draw)
        {
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString("implanter-set-draw-verb"),
                Act = () => TryOpenUi(ent.AsNullable(), user)
            });
        }
    }

    private void OnUseInHand(Entity<ImplanterComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.CurrentMode == ImplanterToggleMode.Draw)
            TryOpenUi(ent.AsNullable(), args.User);
    }

    private void OnSelected(Entity<ImplanterComponent> ent, ref DeimplantChangeVerbMessage args)
    {
        ent.Comp.DeimplantChosen = args.Implant;
        Dirty(ent);
        SetSelectedDeimplant(ent.AsNullable(), args.Implant);
    }

    private void TryOpenUi(Entity<ImplanterComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        _ui.TryToggleUi(ent.Owner, DeimplantUiKey.Key, user);
        ent.Comp.DeimplantChosen ??= ent.Comp.DeimplantWhitelist.FirstOrNull();
        Dirty(ent);
    }

    /// <summary>
    /// Checks if the target already has the same implant prototype implanted.
    /// </summary>
    public bool CheckSameImplant(EntityUid target, EntityUid implant)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return false;

        var implantPrototype = Prototype(implant);
        return implanted.ImplantContainer.ContainedEntities.Any(entity => Prototype(entity) == implantPrototype);
    }

    /// <summary>
    /// Attempt to implant someone else with a doafter.
    /// </summary>
    public void TryImplant(Entity<ImplanterComponent> ent, EntityUid user, EntityUid target)
    {
        var args = new DoAfterArgs(EntityManager, user, ent.Comp.ImplantTime, new ImplantEvent(), ent, target: target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        _popup.PopupClient(Loc.GetString("injector-component-needle-injecting-user"), target, user);

        if (user != target)
        {
            var userName = Identity.Entity(user, EntityManager);
            _popup.PopupEntity(Loc.GetString("implanter-component-implanting-target", ("user", userName)), user, target);
        }
    }

    /// <summary>
    /// Try to remove an implant and store it in an implanter
    /// </summary>
    // TODO: Remove when surgery is in
    public void TryDraw(Entity<ImplanterComponent> ent, EntityUid user, EntityUid target)
    {
        var args = new DoAfterArgs(EntityManager, user, ent.Comp.DrawTime, new DrawEvent(), ent, target: target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        _popup.PopupClient(Loc.GetString("injector-component-needle-injecting-user"), target, user);

        if (user != target)
        {
            var userName = Identity.Entity(user, EntityManager);
            _popup.PopupEntity(Loc.GetString("implanter-component-draw-target", ("user", userName)), user, target);
        }
    }

    private void OnImplant(Entity<ImplanterComponent> ent, ref ImplantEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        Implant(args.User, args.Target.Value, ent);

        args.Handled = true;
    }

    private void OnDraw(Entity<ImplanterComponent> ent, ref DrawEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used == null || args.Target == null)
            return;

        Draw(ent, args.User, args.Target.Value);

        args.Handled = true;
    }

    /// <summary>
    /// Instantly implant something and add all necessary components and containers.
    /// Set to draw mode if not implant only
    /// </summary>
    public void Implant(EntityUid user, EntityUid target, Entity<ImplanterComponent> ent)
    {
        if (!CanImplant(user, target, ent, out var implant, out _))
            return;

        // Check if we are trying to implant a implant which is already implanted
        // Check AFTER the doafter to prevent "is it a fake?" metagaming against deceptive implants
        if (!ent.Comp.AllowMultipleImplants && CheckSameImplant(target, implant.Value))
        {
            var name = Identity.Name(target, EntityManager, user);
            var msg = Loc.GetString("implanter-component-implant-already", ("implant", implant), ("target", name));
            _popup.PopupClient(msg, target, user);
            return;
        }

        // If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        if (ent.Comp.ImplanterSlot.ContainerSlot != null)
            _container.Remove(implant.Value, ent.Comp.ImplanterSlot.ContainerSlot);
        implantContainer.OccludesLight = false;
        _container.Insert(implant.Value, implantContainer);

        if (ent.Comp is { CurrentMode: ImplanterToggleMode.Inject, ImplantOnly: false })
            DrawMode(ent);
        else
            ImplantMode(ent);

        var ev = new TransferDnaEvent { Donor = target, Recipient = ent.Owner };
        RaiseLocalEvent(target, ref ev);

        Dirty(ent);
    }

    /// <summary>
    /// Checks if an implant can be implanted into the target.
    /// </summary>
    /// <returns>True if the implant can be implanted</returns>
    public bool CanImplant(
        EntityUid user,
        EntityUid target,
        Entity<ImplanterComponent> ent,
        [NotNullWhen(true)] out EntityUid? implant,
        [NotNullWhen(true)] out SubdermalImplantComponent? implantComp)
    {
        implant = ent.Comp.ImplanterSlot.ContainerSlot?.ContainedEntities.FirstOrNull();
        if (!TryComp(implant, out implantComp))
            return false;

        if (!CheckTarget(target, ent.Comp.Whitelist, ent.Comp.Blacklist) ||
            !CheckTarget(target, implantComp.Whitelist, implantComp.Blacklist))
        {
            return false;
        }

        var ev = new AddImplantAttemptEvent(user, target, implant.Value, ent.Owner);
        RaiseLocalEvent(target, ev);
        return !ev.Cancelled;
    }

    /// <summary>
    /// Checks if the target passes the whitelist and blacklist checks.
    /// </summary>
    /// <returns>True if the target passes the checks</returns>
    protected bool CheckTarget(EntityUid target, EntityWhitelist? whitelist, EntityWhitelist? blacklist)
    {
        return _whitelist.CheckBoth(target, blacklist, whitelist);
    }

    /// <summary>
    /// Draw the implant out of the target.
    /// TODO: Rework when surgery is in so implant cases can be a thing.
    /// </summary>
    public void Draw(Entity<ImplanterComponent> ent, EntityUid user, EntityUid target)
    {
        var implanterContainer = ent.Comp.ImplanterSlot.ContainerSlot;

        if (implanterContainer is null)
            return;

        var permanentFound = false;

        if (_container.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer))
        {
            if (ent.Comp.AllowDeimplantAll)
            {
                foreach (var implant in implantContainer.ContainedEntities)
                {
                    if (!_implantCompQuery.TryGetComponent(implant, out var implantComp))
                        continue;

                    // Don't remove a permanent implant and look for the next that can be drawn
                    if (!_container.CanRemove(implant, implantContainer))
                    {
                        DrawPermanentFailurePopup(implant, target, user);
                        permanentFound = implantComp.Permanent;
                        continue;
                    }

                    DrawImplantIntoImplanter(ent.Owner, target, implant, implantContainer, implanterContainer);
                    permanentFound = implantComp.Permanent;

                    // Break so only one implant is drawn
                    break;
                }

                if (ent.Comp is { CurrentMode: ImplanterToggleMode.Draw, ImplantOnly: false } && !permanentFound)
                    ImplantMode(ent);
            }
            else
            {
                EntityUid? implant = null;
                var implants = implantContainer.ContainedEntities;
                foreach (var implantEntity in implants)
                {
                    if (!TryComp<SubdermalImplantComponent>(implantEntity, out var subdermalComp))
                        continue;

                    if (ent.Comp.DeimplantChosen == subdermalComp.DrawableProtoIdOverride
                        || Prototype(implantEntity) != null && ent.Comp.DeimplantChosen == Prototype(implantEntity)!)
                    {
                        implant = implantEntity;
                    }
                }

                if (implant != null && _implantCompQuery.TryGetComponent(implant, out var implantComp))
                {
                    // Don't remove a permanent implant
                    if (!_container.CanRemove(implant.Value, implantContainer))
                    {
                        DrawPermanentFailurePopup(implant.Value, target, user);
                        permanentFound = implantComp.Permanent;
                    }
                    else
                    {
                        DrawImplantIntoImplanter(ent.Owner, target, implant.Value, implantContainer, implanterContainer);
                        permanentFound = implantComp.Permanent;
                    }

                    if (ent.Comp is { CurrentMode: ImplanterToggleMode.Draw, ImplantOnly: false } && !permanentFound)
                        ImplantMode(ent);
                }
                else
                {
                    DrawCatastrophicFailure(ent, user);
                }
            }

            Dirty(ent);
        }
        else
        {
            DrawCatastrophicFailure(ent, user);
        }
    }

    /// <summary>
    /// Shows a popup when trying to draw a permanent implant.
    /// </summary>
    private void DrawPermanentFailurePopup(EntityUid implant, EntityUid target, EntityUid user)
    {
        var implantName = Identity.Entity(implant, EntityManager);
        var targetName = Identity.Entity(target, EntityManager);
        var failedPermanentMessage = Loc.GetString("implanter-draw-failed-permanent",
            ("implant", implantName),
            ("target", targetName));
        _popup.PopupClient(failedPermanentMessage, target, user);
    }

    /// <summary>
    /// Moves the implant from the target into the implanter.
    /// </summary>
    private void DrawImplantIntoImplanter(EntityUid implanter, EntityUid target, EntityUid implant, BaseContainer implantContainer, ContainerSlot implanterContainer)
    {
        _container.Remove(implant, implantContainer);
        _container.Insert(implant, implanterContainer);

        var ev = new TransferDnaEvent { Donor = target, Recipient = implanter };
        RaiseLocalEvent(target, ref ev);
    }

    /// <summary>
    /// Handles catastrophic failure when drawing an implant.
    /// </summary>
    private void DrawCatastrophicFailure(Entity<ImplanterComponent> ent, EntityUid user)
    {
        _damageable.TryChangeDamage(user, ent.Comp.DeimplantFailureDamage, ignoreResistances: true, origin: ent.Owner);
        var userName = Identity.Entity(user, EntityManager);
        var failedCatastrophicallyMessage = Loc.GetString("implanter-draw-failed-catastrophically", ("user", userName));
        _popup.PopupPredicted(failedCatastrophicallyMessage, user, user, PopupType.MediumCaution);
        _audio.PlayPredicted(ent.Comp.ImplanterDrawFailSound, ent, user);
    }

    /// <summary>
    /// Switches the implanter to implant mode.
    /// </summary>
    private void ImplantMode(Entity<ImplanterComponent> ent)
    {
        ent.Comp.CurrentMode = ImplanterToggleMode.Inject;
        Dirty(ent);
        ChangeOnImplantVisualizer(ent);
    }

    /// <summary>
    /// Switches the implanter to draw mode.
    /// </summary>
    private void DrawMode(Entity<ImplanterComponent> ent)
    {
        ent.Comp.CurrentMode = ImplanterToggleMode.Draw;
        Dirty(ent);
        ChangeOnImplantVisualizer(ent);
    }

    /// <summary>
    /// Updates the visualizer based on the implant state.
    /// </summary>
    private void ChangeOnImplantVisualizer(Entity<ImplanterComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var implantFound = ent.Comp.ImplanterSlot.HasItem;
        switch (ent.Comp.CurrentMode)
        {
            case ImplanterToggleMode.Inject when !ent.Comp.ImplantOnly:
                _appearance.SetData(ent.Owner, ImplanterVisuals.Full, implantFound, appearance);
                break;
            case ImplanterToggleMode.Inject when ent.Comp.ImplantOnly:
                _appearance.SetData(ent.Owner, ImplanterVisuals.Full, implantFound, appearance);
                _appearance.SetData(ent.Owner, ImplanterImplantOnlyVisuals.ImplantOnly, ent.Comp.ImplantOnly, appearance);
                break;
            default:
                _appearance.SetData(ent.Owner, ImplanterVisuals.Full, implantFound, appearance);
                break;
        }
    }

    /// <summary>
    /// Sets the selected deimplant in the UI.
    /// </summary>
    public void SetSelectedDeimplant(Entity<ImplanterComponent?> ent, string? implant)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (implant != null && _proto.TryIndex<EntityPrototype>(implant, out var proto)) // TODO: Why???
            ent.Comp.DeimplantChosen = proto;

        UpdateUi(ent!);
        Dirty(ent);
    }

    /// <summary>
    /// Update the BUI and status control.
    /// </summary>
    protected virtual void UpdateUi(Entity<ImplanterComponent> ent) { }
}

/// <summary>
/// Event raised when an implantation doafter is complete.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ImplantEvent : SimpleDoAfterEvent;

/// <summary>
/// Event raised when a draw (implant extraction) doafter is complete.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DrawEvent : SimpleDoAfterEvent;

/// <summary>
/// Event raised when an implant is being added to check if it should be cancelled.
/// </summary>
public sealed class AddImplantAttemptEvent(EntityUid user, EntityUid target, EntityUid implant, EntityUid implanter)
    : CancellableEntityEventArgs
{
    public readonly EntityUid User = user;
    public readonly EntityUid Target = target;
    public readonly EntityUid Implant = implant;
    public readonly EntityUid Implanter = implanter;
}

/// <summary>
/// Change the chosen implanter in the UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class DeimplantChangeVerbMessage(string? implant) : BoundUserInterfaceMessage
{
    public readonly string? Implant = implant;
}

[Serializable, NetSerializable]
public enum DeimplantUiKey : byte
{
    Key
}
