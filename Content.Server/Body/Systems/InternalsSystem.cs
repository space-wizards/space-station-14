using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Popups;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Internals;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Body.Systems;

public sealed class InternalsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;

    private EntityQuery<InternalsComponent> _internalsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _internalsQuery = GetEntityQuery<InternalsComponent>();

        SubscribeLocalEvent<InternalsComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<InternalsComponent, ComponentStartup>(OnInternalsStartup);
        SubscribeLocalEvent<InternalsComponent, ComponentShutdown>(OnInternalsShutdown);
        SubscribeLocalEvent<InternalsComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
        SubscribeLocalEvent<InternalsComponent, InternalsDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<InternalsComponent, ToggleInternalsAlertEvent>(OnToggleInternalsAlert);

        SubscribeLocalEvent<InternalsComponent, StartingGearEquippedEvent>(OnStartingGear);
    }

    private void OnStartingGear(EntityUid uid, InternalsComponent component, ref StartingGearEquippedEvent args)
    {
        if (component.BreathTools.Count == 0)
            return;

        if (component.GasTankEntity != null)
            return; // already connected

        // Can the entity breathe the air it is currently exposed to?
        if (_respirator.CanMetabolizeInhaledAir(uid))
            return;

        var tank = FindBestGasTank(uid);
        if (tank == null)
            return;

        // Could the entity metabolise the air in the linked gas tank?
        if (!_respirator.CanMetabolizeGas(uid, tank.Value.Comp.Air))
            return;

        ToggleInternals(uid, uid, force: false, component);
    }

    private void OnGetInteractionVerbs(
        Entity<InternalsComponent> ent,
        ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        if (!AreInternalsWorking(ent) && ent.Comp.BreathTools.Count == 0)
            return;

        var user = args.User;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                ToggleInternals(ent, user, force: false, ent);
            },
            Message = Loc.GetString("action-description-internals-toggle"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Text = Loc.GetString("action-name-internals-toggle"),
        };

        args.Verbs.Add(verb);
    }

    public void ToggleInternals(
        EntityUid uid,
        EntityUid user,
        bool force,
        InternalsComponent? internals = null)
    {
        if (!Resolve(uid, ref internals, logMissing: false))
            return;

        // Toggle off if they're on
        if (AreInternalsWorking(internals))
        {
            if (force)
            {
                DisconnectTank((uid, internals));
                return;
            }

            StartToggleInternalsDoAfter(user, (uid, internals));
            return;
        }

        // If they're not on then check if we have a mask to use
        if (internals.BreathTools.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("internals-no-breath-tool"), uid, user);
            return;
        }

        var tank = FindBestGasTank(uid);

        if (tank is null)
        {
            _popupSystem.PopupEntity(Loc.GetString("internals-no-tank"), uid, user);
            return;
        }

        if (!force)
        {
            StartToggleInternalsDoAfter(user, (uid, internals));
            return;
        }

        _gasTank.ConnectToInternals(tank.Value);
    }

    private void StartToggleInternalsDoAfter(EntityUid user, Entity<InternalsComponent> targetEnt)
    {
        // Is the target not you? If yes, use a do-after to give them time to respond.
        var isUser = user == targetEnt.Owner;
        var delay = !isUser ? targetEnt.Comp.Delay : TimeSpan.Zero;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new InternalsDoAfterEvent(), targetEnt, target: targetEnt)
        {
            BreakOnDamage = true,
            BreakOnMove =  true,
            MovementThreshold = 0.1f,
        });
    }

    private void OnDoAfter(Entity<InternalsComponent> ent, ref InternalsDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        ToggleInternals(ent, args.User, force: true, ent);

        args.Handled = true;
    }

    private void OnToggleInternalsAlert(Entity<InternalsComponent> ent, ref ToggleInternalsAlertEvent args)
    {
        if (args.Handled)
            return;
        ToggleInternals(ent, ent, false, internals: ent.Comp);
        args.Handled = true;
    }

    private void OnInternalsStartup(Entity<InternalsComponent> ent, ref ComponentStartup args)
    {
        _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
    }

    private void OnInternalsShutdown(Entity<InternalsComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent, ent.Comp.InternalsAlert);
    }

    private void OnInhaleLocation(Entity<InternalsComponent> ent, ref InhaleLocationEvent args)
    {
        if (AreInternalsWorking(ent))
        {
            var gasTank = Comp<GasTankComponent>(ent.Comp.GasTankEntity!.Value);
            args.Gas = _gasTank.RemoveAirVolume((ent.Comp.GasTankEntity.Value, gasTank), Atmospherics.BreathVolume);
            // TODO: Should listen to gas tank updates instead I guess?
            _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
        }
    }
    public void DisconnectBreathTool(Entity<InternalsComponent> ent, EntityUid toolEntity)
    {
        ent.Comp.BreathTools.Remove(toolEntity);

        if (TryComp(toolEntity, out BreathToolComponent? breathTool))
            _atmos.DisconnectInternals((toolEntity, breathTool));

        if (ent.Comp.BreathTools.Count == 0)
            DisconnectTank(ent);

        _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
    }

    public void ConnectBreathTool(Entity<InternalsComponent> ent, EntityUid toolEntity)
    {
        if (!ent.Comp.BreathTools.Add(toolEntity))
            return;

        _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
    }

    public void DisconnectTank(Entity<InternalsComponent> ent)
    {
        if (TryComp(ent.Comp.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((ent.Comp.GasTankEntity.Value, tank));

        ent.Comp.GasTankEntity = null;
        _alerts.ShowAlert(ent.Owner, ent.Comp.InternalsAlert, GetSeverity(ent.Comp));
    }

    public bool TryConnectTank(Entity<InternalsComponent> ent, EntityUid tankEntity)
    {
        if (ent.Comp.BreathTools.Count == 0)
            return false;

        if (TryComp(ent.Comp.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((ent.Comp.GasTankEntity.Value, tank));

        ent.Comp.GasTankEntity = tankEntity;
        _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
        return true;
    }

    public bool AreInternalsWorking(EntityUid uid, InternalsComponent? component = null)
    {
        return Resolve(uid, ref component, logMissing: false)
            && AreInternalsWorking(component);
    }

    public bool AreInternalsWorking(InternalsComponent component)
    {
        return TryComp(component.BreathTools.FirstOrNull(), out BreathToolComponent? breathTool)
            && breathTool.IsFunctional
            && HasComp<GasTankComponent>(component.GasTankEntity);
    }

    private short GetSeverity(InternalsComponent component)
    {
        if (component.BreathTools.Count == 0 || !AreInternalsWorking(component))
            return 2;

        // If pressure in the tank is below low pressure threshold, flash warning on internals UI
        if (TryComp<GasTankComponent>(component.GasTankEntity, out var gasTank)
            && gasTank.IsLowPressure)
        {
            return 0;
        }

        return 1;
    }

    public Entity<GasTankComponent>? FindBestGasTank(
        Entity<HandsComponent?, InventoryComponent?, ContainerManagerComponent?> user)
    {
        // TODO use _respirator.CanMetabolizeGas() to prioritize metabolizable gasses
        // Prioritise
        // 1. back equipped tanks
        // 2. exo-slot tanks
        // 3. in-hand tanks
        // 4. pocket/belt tanks

        if (!Resolve(user, ref user.Comp2, ref user.Comp3))
            return null;

        if (_inventory.TryGetSlotEntity(user, "back", out var backEntity, user.Comp2, user.Comp3) &&
            TryComp<GasTankComponent>(backEntity, out var backGasTank) &&
            _gasTank.CanConnectToInternals((backEntity.Value, backGasTank)))
        {
            return (backEntity.Value, backGasTank);
        }

        if (_inventory.TryGetSlotEntity(user, "suitstorage", out var entity, user.Comp2, user.Comp3) &&
            TryComp<GasTankComponent>(entity, out var gasTank) &&
            _gasTank.CanConnectToInternals((entity.Value, gasTank)))
        {
            return (entity.Value, gasTank);
        }

        foreach (var item in _inventory.GetHandOrInventoryEntities((user.Owner, user.Comp1, user.Comp2)))
        {
            if (TryComp(item, out gasTank) && _gasTank.CanConnectToInternals((item, gasTank)))
                return (item, gasTank);
        }

        return null;
    }
}
