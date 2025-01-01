using System.Linq;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Implants;

public sealed partial class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeImplanted();

        SubscribeLocalEvent<ImplanterComponent, AfterInteractEvent>(OnImplanterAfterInteract);

        SubscribeLocalEvent<ImplanterComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
        SubscribeLocalEvent<ImplanterComponent, UseInHandEvent>(OnUseInHand);

        SubscribeLocalEvent<ImplanterComponent, ImplantEvent>(OnImplant);
        SubscribeLocalEvent<ImplanterComponent, DrawEvent>(OnDraw);
        SubscribeLocalEvent<ImplanterComponent, DeimplantChangeVerbMessage>(OnSelected);
    }

    private void OnImplanterAfterInteract(EntityUid uid, ImplanterComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || args.Handled)
            return;

        var target = args.Target.Value;
        if (!CheckTarget(target, component.Whitelist, component.Blacklist))
            return;

        //TODO: Rework when surgery is in for implant cases
        if (component.CurrentMode == ImplanterToggleMode.Draw && !component.ImplantOnly)
        {
            TryDraw(component, args.User, target, uid);
        }
        else
        {
            if (!CanImplant(args.User, target, uid, component, out var implant, out _))
            {
                // no popup if implant doesn't exist
                if (implant == null)
                    return;

                // show popup to the user saying implant failed
                var name = Identity.Name(target, EntityManager, args.User);
                var msg = Loc.GetString("implanter-component-implant-failed", ("implant", implant), ("target", name));
                _popup.PopupEntity(msg, target, args.User);
                // prevent further interaction since popup was shown
                args.Handled = true;
                return;
            }

            // Check if we are trying to implant a implant which is already implanted
            if (implant.HasValue && !component.AllowMultipleImplants && CheckSameImplant(target, implant.Value))
            {
                var name = Identity.Name(target, EntityManager, args.User);
                var msg = Loc.GetString("implanter-component-implant-already", ("implant", implant), ("target", name));
                _popup.PopupEntity(msg, target, args.User);
                args.Handled = true;
                return;
            }


            //Implant self instantly, otherwise try to inject the target.
            if (args.User == target)
                Implant(target, target, uid, component);
            else
                TryImplant(component, args.User, target, uid);
        }

        args.Handled = true;
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

    private void TryOpenUi(EntityUid uid, EntityUid user, ImplanterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        _uiSystem.TryToggleUi(uid, DeimplantUiKey.Key, user);
        UpdateUi(uid, component);
    }

    /// <summary>
    /// Sets selectable implants in the UI of the draw setting window
    /// </summary>
    private void UpdateUi(EntityUid uid, ImplanterComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        Dictionary<string, string> implants = new();

        foreach (var implant in component.DeimplantWhitelist)
        {
            if (_proto.TryIndex(implant, out var proto))
                implants.Add(proto.ID, proto.Name);
        }

        component.DeimplantChosen ??= component.DeimplantWhitelist.FirstOrNull();

        var state = new DeimplantBuiState(component.DeimplantChosen, implants);
        _uiSystem.SetUiState(uid, DeimplantUiKey.Key, state);
    }

    private void OnSelected(EntityUid uid, ImplanterComponent component, DeimplantChangeVerbMessage args)
    {
        component.DeimplantChosen = args.Implant;
        SetSelectedDeimplant(uid, args.Implant, component: component);
    }

    public void SetSelectedDeimplant(EntityUid uid, string? implant, ImplanterComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (implant != null && _proto.TryIndex(implant, out EntityPrototype? proto))
            component.DeimplantChosen = proto;

        Dirty(uid, component);
    }

    /// <summary>
    /// Returns true if the target already has an implant of the same type.
    /// </summary>
    public bool CheckSameImplant(EntityUid target, EntityUid implant)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return false;

        var implantPrototype = Prototype(implant);
        return implanted.ImplantContainer.ContainedEntities.Any(entity => Prototype(entity) == implantPrototype);
    }

    /// <summary>
    /// Attempt to implant someone else.
    /// </summary>
    /// <param name="component">Implanter component</param>
    /// <param name="user">The entity using the implanter</param>
    /// <param name="target">The entity being implanted</param>
    /// <param name="implanter">The implanter being used</param>
    public void TryImplant(ImplanterComponent component, EntityUid user, EntityUid target, EntityUid implanter)
    {
        var args = new DoAfterArgs(EntityManager, user, component.ImplantTime, new ImplantEvent(), implanter, target: target, used: implanter)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

        var userName = Identity.Entity(user, EntityManager);
        _popup.PopupEntity(Loc.GetString("implanter-component-implanting-target", ("user", userName)), user, target, PopupType.LargeCaution);
    }

    /// <summary>
    /// Try to remove an implant and store it in an implanter
    /// </summary>
    /// <param name="component">Implanter component</param>
    /// <param name="user">The entity using the implanter</param>
    /// <param name="target">The entity getting their implant removed</param>
    /// <param name="implanter">The implanter being used</param>
    //TODO: Remove when surgery is in
    public void TryDraw(ImplanterComponent component, EntityUid user, EntityUid target, EntityUid implanter)
    {
        var args = new DoAfterArgs(EntityManager, user, component.DrawTime, new DrawEvent(), implanter, target: target, used: implanter)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (_doAfter.TryStartDoAfter(args))
            _popup.PopupEntity(Loc.GetString("injector-component-injecting-user"), target, user);

    }

    private void OnImplant(EntityUid uid, ImplanterComponent component, ImplantEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        Implant(args.User, args.Target.Value, args.Used.Value, component);

        args.Handled = true;
    }

    private void OnDraw(EntityUid uid, ImplanterComponent component, DrawEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used == null || args.Target == null)
            return;

        Draw(args.Used.Value, args.User, args.Target.Value, component);

        args.Handled = true;
    }
}
