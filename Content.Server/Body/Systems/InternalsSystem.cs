using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.DoAfter;
using Content.Shared.Internals;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Body.Systems;

public sealed class InternalsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalsComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<InternalsComponent, ComponentStartup>(OnInternalsStartup);
        SubscribeLocalEvent<InternalsComponent, ComponentShutdown>(OnInternalsShutdown);
        SubscribeLocalEvent<InternalsComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<InternalsComponent, InternalsDoAfterEvent>(OnDoAfter);
    }

    private void OnGetInteractionVerbs(EntityUid uid, InternalsComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                ToggleInternals(uid, args.User, false, component);
            },
            Message = Loc.GetString("action-description-internals-toggle"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Text = Loc.GetString("action-name-internals-toggle"),
        };

        args.Verbs.Add(verb);
    }

    public void ToggleInternals(EntityUid uid, EntityUid user, bool force, InternalsComponent? internals = null)
    {
        if (!Resolve(uid, ref internals, false))
            return;

        // Toggle off if they're on
        if (AreInternalsWorking(internals))
        {
            if (force || user == uid)
            {
                DisconnectTank(internals);
                return;
            }

            StartToggleInternalsDoAfter(user, uid, internals);
            return;
        }

        // If they're not on then check if we have a mask to use
        if (internals.BreathToolEntity == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("internals-no-breath-tool"), uid, user);
            return;
        }

        var tank = FindBestGasTank(uid ,internals);

        if (tank == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("internals-no-tank"), uid, user);
            return;
        }

        if (!force)
        {
            StartToggleInternalsDoAfter(user, uid, internals);
            return;
        }

        _gasTank.ConnectToInternals(tank);
    }

    private void StartToggleInternalsDoAfter(EntityUid user, EntityUid target, InternalsComponent internals)
    {
        // Is the target not you? If yes, use a do-after to give them time to respond.
        // If not, do a short delay. There's no reason it should be beyond 1 second.
        var isUser = user == target;
        var delay = !isUser ? internals.Delay : 1.0f;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new InternalsDoAfterEvent(), target, target: target)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 0.1f,
        });
    }

    private void OnDoAfter(EntityUid uid, InternalsComponent component, InternalsDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        ToggleInternals(uid, args.User, true, component);

        args.Handled = true;
    }

    private void OnInternalsStartup(EntityUid uid, InternalsComponent component, ComponentStartup args)
    {
        _alerts.ShowAlert(uid, AlertType.Internals, GetSeverity(component));
    }

    private void OnInternalsShutdown(EntityUid uid, InternalsComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, AlertType.Internals);
    }

    private void OnInhaleLocation(EntityUid uid, InternalsComponent component, InhaleLocationEvent args)
    {
        if (AreInternalsWorking(component))
        {
            var gasTank = Comp<GasTankComponent>(component.GasTankEntity!.Value);
            args.Gas = _gasTank.RemoveAirVolume(gasTank, Atmospherics.BreathVolume);
            // TODO: Should listen to gas tank updates instead I guess?
            _alerts.ShowAlert(uid, AlertType.Internals, GetSeverity(component));
        }
    }
    public void DisconnectBreathTool(InternalsComponent component)
    {
        var old = component.BreathToolEntity;
        component.BreathToolEntity = null;

        if (TryComp(old, out BreathToolComponent? breathTool) )
        {
            _atmos.DisconnectInternals(breathTool);
            DisconnectTank(component);
        }

        _alerts.ShowAlert(component.Owner, AlertType.Internals, GetSeverity(component));
    }

    public void ConnectBreathTool(InternalsComponent component, EntityUid toolEntity)
    {
        if (TryComp(component.BreathToolEntity, out BreathToolComponent? tool))
        {
            _atmos.DisconnectInternals(tool);
        }

        component.BreathToolEntity = toolEntity;
        _alerts.ShowAlert(component.Owner, AlertType.Internals, GetSeverity(component));
    }

    public void DisconnectTank(InternalsComponent? component)
    {
        if (component == null)
            return;

        if (TryComp(component.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals(tank);

        component.GasTankEntity = null;
        _alerts.ShowAlert(component.Owner, AlertType.Internals, GetSeverity(component));
    }

    public bool TryConnectTank(InternalsComponent component, EntityUid tankEntity)
    {
        if (component.BreathToolEntity == null)
            return false;

        if (TryComp(component.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals(tank);

        component.GasTankEntity = tankEntity;
        _alerts.ShowAlert(component.Owner, AlertType.Internals, GetSeverity(component));
        return true;
    }

    public bool AreInternalsWorking(InternalsComponent component)
    {
        return TryComp(component.BreathToolEntity, out BreathToolComponent? breathTool) &&
               breathTool.IsFunctional &&
               TryComp(component.GasTankEntity, out GasTankComponent? _);
    }

    private short GetSeverity(InternalsComponent component)
    {
        if (component.BreathToolEntity == null || !AreInternalsWorking(component))
            return 2;

        // If pressure in the tank is below low pressure threshhold, flash warning on internals UI
        if (TryComp<GasTankComponent>(component.GasTankEntity, out var gasTank) && gasTank.IsLowPressure)
            return 0;

        return 1;
    }

    public GasTankComponent? FindBestGasTank(EntityUid internalsOwner, InternalsComponent component)
    {
        // Prioritise
        // 1. back equipped tanks
        // 2. exo-slot tanks
        // 3. in-hand tanks
        // 4. pocket/belt tanks
        InventoryComponent? inventory = null;
        ContainerManagerComponent? containerManager = null;

        if (_inventory.TryGetSlotEntity(internalsOwner, "back", out var backEntity, inventory, containerManager) &&
            TryComp<GasTankComponent>(backEntity, out var backGasTank) &&
            _gasTank.CanConnectToInternals(backGasTank))
        {
            return backGasTank;
        }

        if (_inventory.TryGetSlotEntity(internalsOwner, "suitstorage", out var entity, inventory, containerManager) &&
            TryComp<GasTankComponent>(entity, out var gasTank) &&
            _gasTank.CanConnectToInternals(gasTank))
        {
            return gasTank;
        }

        var tanks = new List<GasTankComponent>();

        foreach (var hand in _hands.EnumerateHands(internalsOwner))
        {
            if (TryComp(hand.HeldEntity, out gasTank) && _gasTank.CanConnectToInternals(gasTank))
                tanks.Add(gasTank);
        }

        if (tanks.Count > 0)
        {
            tanks.Sort((x, y) => y.Air.TotalMoles.CompareTo(x.Air.TotalMoles));
            return tanks[0];
        }

        if (Resolve(internalsOwner, ref inventory, false))
        {
            var enumerator = new InventorySystem.ContainerSlotEnumerator(internalsOwner, inventory.TemplateId, _protoManager, _inventory, SlotFlags.POCKET | SlotFlags.BELT);

            while (enumerator.MoveNext(out var container))
            {
                if (TryComp(container.ContainedEntity, out gasTank) && _gasTank.CanConnectToInternals(gasTank))
                    tanks.Add(gasTank);
            }

            if (tanks.Count > 0)
            {
                tanks.Sort((x, y) => y.Air.TotalMoles.CompareTo(x.Air.TotalMoles));
                return tanks[0];
            }
        }

        return null;
    }
}
