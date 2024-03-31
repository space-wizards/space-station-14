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

    private void OnGetInteractionVerbs(
        Entity<InternalsComponent> ent,
        ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
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
            if (force || user == uid)
            {
                DisconnectTank(internals);
                return;
            }

            StartToggleInternalsDoAfter(user, (uid, internals));
            return;
        }

        // If they're not on then check if we have a mask to use
        if (internals.BreathToolEntity is null)
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

    private void OnInternalsStartup(Entity<InternalsComponent> ent, ref ComponentStartup args)
    {
        _alerts.ShowAlert(ent, AlertType.Internals, GetSeverity(ent));
    }

    private void OnInternalsShutdown(Entity<InternalsComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent, AlertType.Internals);
    }

    private void OnInhaleLocation(Entity<InternalsComponent> ent, ref InhaleLocationEvent args)
    {
        if (AreInternalsWorking(ent))
        {
            var gasTank = Comp<GasTankComponent>(ent.Comp.GasTankEntity!.Value);
            args.Gas = _gasTank.RemoveAirVolume((ent.Comp.GasTankEntity.Value, gasTank), Atmospherics.BreathVolume);
            // TODO: Should listen to gas tank updates instead I guess?
            _alerts.ShowAlert(ent, AlertType.Internals, GetSeverity(ent));
        }
    }
    public void DisconnectBreathTool(Entity<InternalsComponent> ent)
    {
        var old = ent.Comp.BreathToolEntity;
        ent.Comp.BreathToolEntity = null;

        if (TryComp(old, out BreathToolComponent? breathTool))
        {
            _atmos.DisconnectInternals(breathTool);
            DisconnectTank(ent);
        }

        _alerts.ShowAlert(ent, AlertType.Internals, GetSeverity(ent));
    }

    public void ConnectBreathTool(Entity<InternalsComponent> ent, EntityUid toolEntity)
    {
        if (TryComp(ent.Comp.BreathToolEntity, out BreathToolComponent? tool))
        {
            _atmos.DisconnectInternals(tool);
        }

        ent.Comp.BreathToolEntity = toolEntity;
        _alerts.ShowAlert(ent, AlertType.Internals, GetSeverity(ent));
    }

    public void DisconnectTank(InternalsComponent? component)
    {
        if (component is null)
            return;

        if (TryComp(component.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((component.GasTankEntity.Value, tank));

        component.GasTankEntity = null;
        _alerts.ShowAlert(component.Owner, AlertType.Internals, GetSeverity(component));
    }

    public bool TryConnectTank(Entity<InternalsComponent> ent, EntityUid tankEntity)
    {
        if (ent.Comp.BreathToolEntity is null)
            return false;

        if (TryComp(ent.Comp.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((ent.Comp.GasTankEntity.Value, tank));

        ent.Comp.GasTankEntity = tankEntity;
        _alerts.ShowAlert(ent, AlertType.Internals, GetSeverity(ent));
        return true;
    }

    public bool AreInternalsWorking(EntityUid uid, InternalsComponent? component = null)
    {
        return Resolve(uid, ref component, logMissing: false)
            && AreInternalsWorking(component);
    }

    public bool AreInternalsWorking(InternalsComponent component)
    {
        return TryComp(component.BreathToolEntity, out BreathToolComponent? breathTool)
            && breathTool.IsFunctional
            && HasComp<GasTankComponent>(component.GasTankEntity);
    }

    private short GetSeverity(InternalsComponent component)
    {
        if (component.BreathToolEntity is null || !AreInternalsWorking(component))
            return 2;

        // If pressure in the tank is below low pressure threshhold, flash warning on internals UI
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
        // Prioritise
        // 1. back equipped tanks
        // 2. exo-slot tanks
        // 3. in-hand tanks
        // 4. pocket/belt tanks

        if (!Resolve(user, ref user.Comp1, ref user.Comp2, ref user.Comp3))
            return null;

        if (_inventory.TryGetSlotEntity(user, "back", out var backEntity, user.Comp2, user.Comp3) &&
            TryComp<GasTankComponent>(backEntity, out var backGasTank) &&
            _gasTank.CanConnectToInternals(backGasTank))
        {
            return (backEntity.Value, backGasTank);
        }

        if (_inventory.TryGetSlotEntity(user, "suitstorage", out var entity, user.Comp2, user.Comp3) &&
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
