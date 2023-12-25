using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
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

    public const SlotFlags InventorySlots = SlotFlags.POCKET | SlotFlags.BELT;

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

        var tank = FindBestGasTank(uid);

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

        _gasTank.ConnectToInternals(tank.Value);
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
            args.Gas = _gasTank.RemoveAirVolume((component.GasTankEntity.Value, gasTank), Atmospherics.BreathVolume);
            // TODO: Should listen to gas tank updates instead I guess?
            _alerts.ShowAlert(uid, AlertType.Internals, GetSeverity(component));
        }
    }
    public void DisconnectBreathTool(Entity<InternalsComponent> ent)
    {
        var (owner, component) = ent;
        var old = component.BreathToolEntity;
        component.BreathToolEntity = null;

        if (TryComp(old, out BreathToolComponent? breathTool) )
        {
            _atmos.DisconnectInternals(breathTool);
            DisconnectTank(ent);
        }

        _alerts.ShowAlert(owner, AlertType.Internals, GetSeverity(component));
    }

    public void ConnectBreathTool(Entity<InternalsComponent> ent, EntityUid toolEntity)
    {
        var (owner, component) = ent;
        if (TryComp(component.BreathToolEntity, out BreathToolComponent? tool))
        {
            _atmos.DisconnectInternals(tool);
        }

        component.BreathToolEntity = toolEntity;
        _alerts.ShowAlert(owner, AlertType.Internals, GetSeverity(component));
    }

    public void DisconnectTank(InternalsComponent? component)
    {
        if (component == null)
            return;

        if (TryComp(component.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((component.GasTankEntity.Value, tank));

        component.GasTankEntity = null;
        _alerts.ShowAlert(component.Owner, AlertType.Internals, GetSeverity(component));
    }

    public bool TryConnectTank(Entity<InternalsComponent> ent, EntityUid tankEntity)
    {
        var component = ent.Comp;
        if (component.BreathToolEntity == null)
            return false;

        if (TryComp(component.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((component.GasTankEntity.Value, tank));

        component.GasTankEntity = tankEntity;
        _alerts.ShowAlert(ent, AlertType.Internals, GetSeverity(component));
        return true;
    }

    public bool AreInternalsWorking(EntityUid uid, InternalsComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return AreInternalsWorking(component);
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

    public Entity<GasTankComponent>? FindBestGasTank(Entity<HandsComponent?, InventoryComponent?, ContainerManagerComponent?> user)
    {
        // Prioritise
        // 1. back equipped tanks
        // 2. exo-slot tanks
        // 3. in-hand tanks
        // 4. pocket/belt tanks

        if (!Resolve(user.Owner, ref user.Comp1, ref user.Comp2, ref user.Comp3))
            return null;

        if (_inventory.TryGetSlotEntity(user.Owner, "back", out var backEntity, user.Comp2, user.Comp3) &&
            TryComp<GasTankComponent>(backEntity, out var backGasTank) &&
            _gasTank.CanConnectToInternals(backGasTank))
        {
            return (backEntity.Value, backGasTank);
        }

        if (_inventory.TryGetSlotEntity(user.Owner, "suitstorage", out var entity, user.Comp2, user.Comp3) &&
            TryComp<GasTankComponent>(entity, out var gasTank) &&
            _gasTank.CanConnectToInternals(gasTank))
        {
            return (entity.Value, gasTank);
        }

        foreach (var item in _inventory.GetHandOrInventoryEntities((user.Owner, user.Comp1, user.Comp2)))
        {
            if (TryComp(item, out gasTank) && _gasTank.CanConnectToInternals(gasTank))
                return (item, gasTank);
        }

        return null;
    }
}
