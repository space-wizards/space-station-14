using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Internals;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Handles lung breathing with gas tanks for entities.
/// </summary>
public abstract class SharedInternalsSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGasTankSystem _gasTank = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InternalsComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);

        SubscribeLocalEvent<InternalsComponent, ComponentStartup>(OnInternalsStartup);
        SubscribeLocalEvent<InternalsComponent, ComponentShutdown>(OnInternalsShutdown);

        SubscribeLocalEvent<InternalsComponent, InternalsDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<InternalsComponent, ToggleInternalsAlertEvent>(OnToggleInternalsAlert);
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

    public bool ToggleInternals(
        EntityUid uid,
        EntityUid user,
        bool force,
        InternalsComponent? internals = null)
    {
        if (!Resolve(uid, ref internals, logMissing: false))
            return false;

        // Check if a mask is present.
        if (internals.BreathTools.Count == 0)
        {
            _popupSystem.PopupClient(Loc.GetString("internals-no-breath-tool"), uid, user);
            return false;
        }

        // Start the toggle do-after if it's on someone else.
        if (!force && user != uid)
        {
            return StartToggleInternalsDoAfter(user, (uid, internals));
        }

        // Toggle off.
        if (TryComp(internals.GasTankEntity, out GasTankComponent? gas))
        {
            return _gasTank.DisconnectFromInternals((internals.GasTankEntity.Value, gas), user);
        }
        else
        {
            // Check if tank is present.
            var tank = FindBestGasTank(uid);

            // If they're not on then check if we have a mask to use
            if (tank == null)
            {
                _popupSystem.PopupClient(Loc.GetString("internals-no-tank"), uid, user);
                return false;
            }

            return _gasTank.ConnectToInternals(tank.Value, user: user);
        }
    }

    private bool StartToggleInternalsDoAfter(EntityUid user, Entity<InternalsComponent> targetEnt)
    {
        // Is the target not you? If yes, use a do-after to give them time to respond.
        var isUser = user == targetEnt.Owner;
        var delay = !isUser ? targetEnt.Comp.Delay : TimeSpan.Zero;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new InternalsDoAfterEvent(), targetEnt, target: targetEnt)
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

        args.Handled |= ToggleInternals(ent, ent, false, internals: ent.Comp);
    }

    private void OnInternalsStartup(Entity<InternalsComponent> ent, ref ComponentStartup args)
    {
        _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
    }

    private void OnInternalsShutdown(Entity<InternalsComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent, ent.Comp.InternalsAlert);
    }

    public void ConnectBreathTool(Entity<InternalsComponent> ent, EntityUid toolEntity)
    {
        if (!ent.Comp.BreathTools.Add(toolEntity))
            return;

        if (TryComp(toolEntity, out BreathToolComponent? breathTool))
        {
            breathTool.ConnectedInternalsEntity = ent.Owner;
            Dirty(toolEntity, breathTool);
        }

        Dirty(ent);
        _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
    }

    public void DisconnectBreathTool(Entity<InternalsComponent> ent, EntityUid toolEntity, bool forced = false)
    {
        if (!ent.Comp.BreathTools.Remove(toolEntity))
            return;

        Dirty(ent);

        if (TryComp(toolEntity, out BreathToolComponent? breathTool))
        {
            breathTool.ConnectedInternalsEntity = null;
            Dirty(toolEntity, breathTool);
        }

        if (ent.Comp.BreathTools.Count == 0)
        {
            DisconnectTank(ent, forced: forced);
        }

        _alerts.ShowAlert(ent, ent.Comp.InternalsAlert, GetSeverity(ent));
    }

    public void DisconnectTank(Entity<InternalsComponent> ent, bool forced = false)
    {
        if (TryComp(ent.Comp.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((ent.Comp.GasTankEntity.Value, tank), forced: forced);

        ent.Comp.GasTankEntity = null;
        Dirty(ent);
        _alerts.ShowAlert(ent.Owner, ent.Comp.InternalsAlert, GetSeverity(ent.Comp));
    }

    public bool TryConnectTank(Entity<InternalsComponent> ent, EntityUid tankEntity)
    {
        if (ent.Comp.BreathTools.Count == 0)
            return false;

        if (TryComp(ent.Comp.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((ent.Comp.GasTankEntity.Value, tank));

        ent.Comp.GasTankEntity = tankEntity;
        Dirty(ent);
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

    protected short GetSeverity(InternalsComponent component)
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
