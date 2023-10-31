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

    private void OnGetInteractionVerbs(Entity<InternalsComponent> internals, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var @event = args;
        InteractionVerb verb = new()
        {
            Act = () =>
            {
                ToggleInternals((internals, internals), @event.User, false);
            },
            Message = Loc.GetString("action-description-internals-toggle"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Text = Loc.GetString("action-name-internals-toggle"),
        };

        args.Verbs.Add(verb);
    }

    public void ToggleInternals(Entity<InternalsComponent?> internals, EntityUid user, bool force)
    {
        if (!Resolve(internals, ref internals.Comp, false))
            return;

        // Toggle off if they're on
        if (AreInternalsWorking(internals.Comp))
        {
            if (force || user == internals.Owner)
            {
                DisconnectTank((internals, internals.Comp));
                return;
            }

            StartToggleInternalsDoAfter(user, internals, internals.Comp);
            return;
        }

        // If they're not on then check if we have a mask to use
        if (internals.Comp.BreathToolEntity == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("internals-no-breath-tool"), internals, user);
            return;
        }

        var tank = FindBestGasTank(internals, internals.Comp);

        if (tank == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("internals-no-tank"), internals, user);
            return;
        }

        if (!force)
        {
            StartToggleInternalsDoAfter(user, internals, internals.Comp);
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

    private void OnDoAfter(Entity<InternalsComponent> internals, ref InternalsDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        ToggleInternals((internals, internals), args.User, true);

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

    public void DisconnectTank(Entity<InternalsComponent>? internals)
    {
        if (internals is not { Comp: var component })
            return;

        if (TryComp(component.GasTankEntity, out GasTankComponent? tank))
            _gasTank.DisconnectFromInternals((component.GasTankEntity.Value, tank));

        component.GasTankEntity = null;
        _alerts.ShowAlert(internals.Value, AlertType.Internals, GetSeverity(internals));
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

    public Entity<GasTankComponent>? FindBestGasTank(EntityUid internalsOwner, InternalsComponent component)
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
            _gasTank.CanConnectToInternals((backEntity.Value, backGasTank)))
        {
            return (backEntity.Value, backGasTank);
        }

        if (_inventory.TryGetSlotEntity(internalsOwner, "suitstorage", out var entity, inventory, containerManager) &&
            TryComp<GasTankComponent>(entity, out var gasTank) &&
            _gasTank.CanConnectToInternals((entity.Value, gasTank)))
        {
            return (entity.Value, gasTank);
        }

        var tanks = new List<Entity<GasTankComponent>>();

        foreach (var hand in _hands.EnumerateHands(internalsOwner))
        {
            if (TryComp(hand.HeldEntity, out gasTank) && _gasTank.CanConnectToInternals((hand.HeldEntity.Value, gasTank)))
                tanks.Add((hand.HeldEntity.Value, gasTank));
        }

        if (tanks.Count > 0)
        {
            tanks.Sort((x, y) => y.Comp.Air.TotalMoles.CompareTo(x.Comp.Air.TotalMoles));
            return tanks[0];
        }

        if (Resolve(internalsOwner, ref inventory, false))
        {
            var enumerator = new InventorySystem.ContainerSlotEnumerator(internalsOwner, inventory.TemplateId, _protoManager, _inventory, SlotFlags.POCKET | SlotFlags.BELT);

            while (enumerator.MoveNext(out var container))
            {
                if (TryComp(container.ContainedEntity, out gasTank) && _gasTank.CanConnectToInternals((container.ContainedEntity.Value, gasTank)))
                    tanks.Add((container.ContainedEntity.Value, gasTank));
            }

            if (tanks.Count > 0)
            {
                tanks.Sort((x, y) => y.Comp.Air.TotalMoles.CompareTo(x.Comp.Air.TotalMoles));
                return tanks[0];
            }
        }

        return null;
    }
}
