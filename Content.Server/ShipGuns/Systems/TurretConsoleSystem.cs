using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.ShipGuns.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.UserInterface;
using Content.Shared.Alert;
using Content.Shared.ShipGuns.Components;
using Content.Shared.ShipGuns.Systems;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.ShipGuns.Systems;

/// <inheritdoc/>
public sealed class TurretConsoleSystem : SharedTurretConsoleSystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurretConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<TurretConsoleComponent, PowerChangedEvent>(OnConsolePowerChange);
        SubscribeLocalEvent<TurretConsoleComponent, AnchorStateChangedEvent>(OnConsoleAnchorChange);
        SubscribeLocalEvent<TurretConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);
        SubscribeLocalEvent<TurretConsoleComponent, BoundUIClosedEvent>(OnConsoleUIClose);

        SubscribeLocalEvent<GunnerComponent, MoveEvent>(HandleGunnerMove);
    }

    private void OnConsoleShutdown(EntityUid uid, TurretConsoleComponent component, ComponentShutdown args)
    {
        if (component.SubscribedGunner == null) return;
        RemoveGunner(component.SubscribedGunner);
    }

    private void OnConsoleAnchorChange(EntityUid uid, TurretConsoleComponent component,
        ref AnchorStateChangedEvent args)
    {
        switch (args.Anchored)
        {
            case false:
            {
                if (component.SubscribedGunner != null)
                    RemoveGunner(component.SubscribedGunner);
                break;
            }
            case true:
            {
                break;
            }
        }
    }

    private void OnConsolePowerChange(EntityUid uid, TurretConsoleComponent component, PowerChangedEvent args)
    {
        switch(args.Powered)
        {
            case false:
            {
                if(component.SubscribedGunner != null)
                    RemoveGunner(component.SubscribedGunner);
                break;
            }
            case true:
            {
                break;
            }
        }
    }

    private void HandleGunnerMove(EntityUid uid, GunnerComponent component, ref MoveEvent args)
    {
        if (component.Console == null || component.Position == null)
        {
            DebugTools.Assert(component.Position == null && component.Console == null);
            EntityManager.RemoveComponent<GunnerComponent>(uid);
            return;
        }

        if (args.NewPosition.TryDistance(EntityManager, component.Position.Value, out var distance) &&
            distance < GunnerComponent.BreakDistance)
            return;

        RemoveGunner(component);
    }

    protected override void HandleGunnerShutdown(EntityUid uid, GunnerComponent component, ComponentShutdown args)
    {
        base.HandleGunnerShutdown(uid, component, args);
        RemoveGunner(component);
    }

    private void OnConsoleUIOpenAttempt(EntityUid uid, TurretConsoleComponent component,
        ActivatableUIOpenAttemptEvent args)
    {
        if(!TryGunner(args.User, uid))
            args.Cancel();
    }

    private void OnConsoleUIClose(EntityUid uid, TurretConsoleComponent component, BoundUIClosedEvent args)
    {
        if ((TurretConsoleUiKey) args.UiKey != TurretConsoleUiKey.Key || args.Session.AttachedEntity is not { } user)
            return;

        RemoveGunner(user);
    }

    private bool TryGunner(EntityUid user, EntityUid console)
    {
        if (!_tags.HasTag(user, "CanUseShipGuns") ||
            !TryComp<TurretConsoleComponent>(console, out var component) ||
            !this.IsPowered(console, EntityManager) ||
            !Transform(console).Anchored ||
            !_actionBlockerSystem.CanInteract(user, console) ||
            component.SubscribedGunner != null)
        {
            return false;
        }

        var gunnerComponent = EntityManager.EnsureComponent<GunnerComponent>(user);
        var consoleComponent = gunnerComponent.Console;

        if (consoleComponent != null)
        {
            RemoveGunner(gunnerComponent);

            if (consoleComponent == component)
            {
                return false;
            }
        }

        AddGunner(user, component);
        return true;
    }

    private void AddGunner(EntityUid entity, TurretConsoleComponent component)
    {
        var gunner = EntityManager.GetComponent<GunnerComponent>(entity);

        if (TryComp<SharedEyeComponent>(entity, out var eye))
        {
            eye.Zoom = component.Zoom;
        }

        component.SubscribedGunner = gunner;

        gunner.Console = component;
        _actionBlockerSystem.UpdateCanMove(entity);
        gunner.Position = Transform(entity).Coordinates;
        Dirty(gunner);
    }

    private void RemoveGunner(EntityUid uid)
    {
        if (!EntityManager.TryGetComponent(uid, out GunnerComponent? component))
            return;

        RemoveGunner(component);
    }

    private void RemoveGunner(GunnerComponent component)
    {
        var console = component.Console;

        if (console is not TurretConsoleComponent gunner)
            return;

        component.Console = null;
        component.Position = null;

        if (TryComp<SharedEyeComponent>(component.Owner, out var eye))
        {
            eye.Zoom = new Vector2(1.0f, 1.0f);
        }

        gunner.SubscribedGunner = null;

        _popupSystem.PopupEntity("COCK", component.Owner, Filter.Entities(component.Owner));

        if (component.LifeStage < ComponentLifeStage.Stopping)
            EntityManager.RemoveComponent<GunnerComponent>(component.Owner);
    }
}
