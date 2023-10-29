using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Ame.Components;
using Content.Server.Chat.Managers;
using Content.Server.NodeContainer;
using Content.Server.Nodes;
using Content.Server.Nodes.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Nodes.Events;
using Content.Server.Power.Components;
using Content.Shared.Ame;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.Ame.EntitySystems;

public sealed class AmeControllerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AmeSystem _ameSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly NodeGraphSystem _nodeSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeControllerComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AmeControllerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AmeControllerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AmeControllerComponent, UiButtonPressedMessage>(OnUiButtonPressed);
        SubscribeLocalEvent<AmeControllerComponent, AddedToGraphEvent>(OnAddedToGraph);
        SubscribeLocalEvent<AmeControllerComponent, RemovedFromGraphEvent>(OnRemovedFromGraph);
        SubscribeLocalEvent<AmeControllerComponent, ProxyNodeRelayEvent<AddedToGraphEvent>>(OnAddedToGraph);
        SubscribeLocalEvent<AmeControllerComponent, ProxyNodeRelayEvent<RemovedFromGraphEvent>>(OnRemovedFromGraph);
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<AmeControllerComponent, PolyNodeComponent>();
        while (query.MoveNext(out var uid, out var controller, out var nodes))
        {
            if (controller.NextUpdate <= curTime)
                UpdateController((uid, controller, nodes), curTime);
            else if (controller.NextUIUpdate <= curTime)
                UpdateUi((uid, controller));
        }
    }

    private void UpdateController(Entity<AmeControllerComponent?, PolyNodeComponent?> controller, TimeSpan curTime)
    {
        if (!Resolve(controller, ref controller.Comp1))
            return;

        controller.Comp1.LastUpdate = curTime;
        controller.Comp1.NextUpdate = curTime + controller.Comp1.UpdatePeriod;
        // update the UI regardless of other factors to update the power readings
        UpdateUi((controller.Owner, controller.Comp1));

        if (!controller.Comp1.Injecting)
            return;
        if (!TryGetAmeGraph((controller.Owner, null, controller.Comp2), out var ame))
            return;

        if (TryComp<AmeFuelContainerComponent>(controller.Comp1.JarSlot.ContainedEntity, out var fuelJar) && fuelJar.FuelAmount > 0)
        {
            var availableInject = Math.Min(controller.Comp1.InjectionAmount, fuelJar.FuelAmount);
            var powerOutput = _ameSystem.InjectFuel((ame.Owner, ame.Comp), availableInject, out var overloading);
            fuelJar.FuelAmount -= availableInject;

            if (TryComp<PowerSupplierComponent>(controller, out var powerOutlet))
                powerOutlet.MaxSupply = powerOutput;

            if (availableInject > 0)
                _audioSystem.PlayPvs(controller.Comp1.InjectSound, controller, AudioParams.Default.WithVolume(overloading ? 10f : 0f));

            UpdateUi((controller.Owner, controller.Comp1));
        }
        else
        {
            SetInjecting((controller.Owner, controller.Comp1), false, null);
        }

        controller.Comp1.Stability = _ameSystem.GetTotalStability((ame.Owner, ame.Comp));

        _ameSystem.UpdateVisuals((ame.Owner, ame.Comp));
        UpdateDisplay((controller.Owner, controller.Comp1, null), controller.Comp1.Stability);

        if (controller.Comp1.Stability <= 0)
            _ameSystem.ExplodeCores((ame.Owner, ame.Comp));
    }

    public void UpdateUi(Entity<AmeControllerComponent?> controller)
    {
        if (!Resolve(controller, ref controller.Comp))
            return;

        if (!_userInterfaceSystem.TryGetUi(controller, AmeControllerUiKey.Key, out var bui))
            return;

        var state = GetUiState((controller.Owner, controller.Comp));
        _userInterfaceSystem.SetUiState(bui, state);

        controller.Comp.NextUIUpdate = _gameTiming.CurTime + controller.Comp.UpdateUIPeriod;
    }

    private AmeControllerBoundUserInterfaceState GetUiState(Entity<AmeControllerComponent> controller)
    {
        var powered = !TryComp<ApcPowerReceiverComponent>(controller, out var powerSource) || powerSource.Powered;
        var coreCount = 0;
        // how much power can be produced at the current settings, in kW
        // we don't use max. here since this is what is set in the Controller, not what the AME is actually producing
        float targetedPowerSupply = 0;
        if (TryGetAmeGraph((controller.Owner, null, null), out var ame))
        {
            coreCount = ame.Comp.Cores.Count;
            targetedPowerSupply = _ameSystem.CalculatePower(controller.Comp.InjectionAmount, coreCount) / 1000;
        }

        // set current power statistics in kW
        float currentPowerSupply = 0;
        if (TryComp<PowerSupplierComponent>(controller, out var powerOutlet) && coreCount > 0)
        {
            currentPowerSupply = powerOutlet.CurrentSupply / 1000;
        }

        var hasJar = Exists(controller.Comp.JarSlot.ContainedEntity);
        if (!hasJar || !TryComp<AmeFuelContainerComponent>(controller.Comp.JarSlot.ContainedEntity, out var jar))
            return new AmeControllerBoundUserInterfaceState(powered, IsMasterController(controller), false, hasJar, 0, controller.Comp.InjectionAmount, coreCount, currentPowerSupply, targetedPowerSupply);

        return new AmeControllerBoundUserInterfaceState(powered, IsMasterController(controller), controller.Comp.Injecting, hasJar, jar.FuelAmount, controller.Comp.InjectionAmount, coreCount, currentPowerSupply, targetedPowerSupply);
    }

    private bool IsMasterController(EntityUid uid)
    {
        return TryGetAmeGraph((uid, null, null), out var ame) && ame.Comp.MasterController == uid;
    }

    private bool TryGetAmeGraph(Entity<GraphNodeComponent?, PolyNodeComponent?> ameNode, out Entity<AmeComponent> ame)
    {
        ame = (EntityUid.Invalid, default!);
        foreach (var graph in _nodeSystem.EnumerateGraphs(ameNode))
        {
            if (!TryComp(graph, out ame.Comp!))
                continue;

            ame.Owner = graph;
            return true;
        }

        return false;
    }

    public void TryEject(Entity<AmeControllerComponent?> controller, EntityUid? user = null)
    {
        if (!Resolve(controller, ref controller.Comp))
            return;
        if (controller.Comp.Injecting)
            return;

        var jar = controller.Comp.JarSlot.ContainedEntity;
        if (!Exists(jar))
            return;

        controller.Comp.JarSlot.Remove(jar!.Value);
        UpdateUi(controller);
        if (Exists(user))
            _handsSystem.PickupOrDrop(user, jar!.Value);
    }

    public void SetInjecting(Entity<AmeControllerComponent?> controller, bool value, EntityUid? user = null)
    {
        if (!Resolve(controller, ref controller.Comp))
            return;
        if (controller.Comp.Injecting == value)
            return;

        controller.Comp.Injecting = value;
        UpdateDisplay((controller.Owner, controller.Comp, null), controller.Comp.Stability);
        if (!value && TryComp<PowerSupplierComponent>(controller, out var powerOut))
            powerOut.MaxSupply = 0;

        UpdateUi(controller);

        // Logging
        if (!HasComp<MindContainerComponent>(user))
            return;

        var humanReadableState = value ? "Inject" : "Not inject";
        _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{EntityManager.ToPrettyString(user.Value):player} has set the AME to {humanReadableState}");
    }

    public void ToggleInjecting(Entity<AmeControllerComponent?> controller, EntityUid? user = null)
    {
        if (!Resolve(controller, ref controller.Comp))
            return;
        SetInjecting(controller, !controller.Comp.Injecting, user);
    }

    public void SetInjectionAmount(Entity<AmeControllerComponent?> controller, int value, EntityUid? user = null)
    {
        if (!Resolve(controller, ref controller.Comp))
            return;
        if (controller.Comp.InjectionAmount == value)
            return;

        var oldValue = controller.Comp.InjectionAmount;
        controller.Comp.InjectionAmount = value;

        UpdateUi(controller);

        // Logging
        if (!HasComp<MindContainerComponent>(user))
            return;

        var humanReadableState = controller.Comp.Injecting ? "Inject" : "Not inject";
        _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{EntityManager.ToPrettyString(user.Value):player} has set the AME to inject {controller.Comp.InjectionAmount} while set to {humanReadableState}");

        // Admin alert
        var safeLimit = 0;
        if (TryGetAmeGraph((controller.Owner, null, null), out var ame))
            safeLimit = ame.Comp.Cores.Count * 2;

        if (oldValue <= safeLimit && value > safeLimit)
            _chatManager.SendAdminAlert(user.Value, $"increased AME over safe limit to {controller.Comp.InjectionAmount}");
    }

    public void AdjustInjectionAmount(Entity<AmeControllerComponent?> controller, int delta, int min = 0, int max = int.MaxValue, EntityUid? user = null)
    {
        if (Resolve(controller, ref controller.Comp))
            SetInjectionAmount(controller, MathHelper.Clamp(controller.Comp.InjectionAmount + delta, min, max), user);
    }

    private void UpdateDisplay(Entity<AmeControllerComponent?, AppearanceComponent?> controller, int stability)
    {
        if (!Resolve(controller, ref controller.Comp1, ref controller.Comp2))
            return;

        var ameControllerState = stability switch
        {
            < 10 => AmeControllerState.Fuck,
            < 50 => AmeControllerState.Critical,
            _ => AmeControllerState.On,
        };

        if (!controller.Comp1.Injecting)
            ameControllerState = AmeControllerState.Off;

        _appearanceSystem.SetData(
            controller.Owner,
            AmeControllerVisuals.DisplayState,
            ameControllerState,
            controller.Comp2
        );
    }

    private void OnComponentStartup(EntityUid uid, AmeControllerComponent comp, ComponentStartup args)
    {
        // TODO: Fix this bad name. I'd update maps but then people get mad.
        comp.JarSlot = _containerSystem.EnsureContainer<ContainerSlot>(uid, AmeControllerComponent.FuelContainerId);
    }

    private void OnInteractUsing(EntityUid uid, AmeControllerComponent comp, InteractUsingEvent args)
    {
        if (!HasComp<HandsComponent>(args.User))
        {
            _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-no-hands-text"), uid, args.User);
            return;
        }

        if (!HasComp<AmeFuelContainerComponent>(args.Used))
        {
            _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-fail"), uid, args.User);
            return;
        }

        if (Exists(comp.JarSlot.ContainedEntity))
        {
            _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-already-has-jar"), uid, args.User);
            return;
        }

        comp.JarSlot.Insert(args.Used);
        _popupSystem.PopupEntity(Loc.GetString("ame-controller-component-interact-using-success"), uid, args.User, PopupType.Medium);

        UpdateUi((uid, comp));
    }

    private void OnPowerChanged(EntityUid uid, AmeControllerComponent comp, ref PowerChangedEvent args)
    {
        UpdateUi((uid, comp));
    }

    private void OnUiButtonPressed(EntityUid uid, AmeControllerComponent comp, UiButtonPressedMessage msg)
    {
        var user = msg.Session.AttachedEntity;
        if (!Exists(user))
            return;

        var needsPower = msg.Button switch
        {
            UiButton.Eject => false,
            _ => true,
        };

        if (!PlayerCanUseController((uid, comp), user!.Value, needsPower))
            return;

        _audioSystem.PlayPvs(comp.ClickSound, uid, AudioParams.Default.WithVolume(-2f));
        switch (msg.Button)
        {
            case UiButton.Eject:
                TryEject((uid, comp), user: user);
                break;
            case UiButton.ToggleInjection:
                ToggleInjecting((uid, comp), user: user);
                break;
            case UiButton.IncreaseFuel:
                AdjustInjectionAmount((uid, comp), +2, user: user);
                break;
            case UiButton.DecreaseFuel:
                AdjustInjectionAmount((uid, comp), -2, user: user);
                break;
        }

        if (TryGetAmeGraph((uid, null, null), out var ame))
            _ameSystem.UpdateVisuals((ame.Owner, ame.Comp));

        UpdateUi((uid, comp));
    }

    /// <summary>
    /// Checks whether the player entity is able to use the controller.
    /// </summary>
    /// <param name="playerEntity">The player entity.</param>
    /// <returns>Returns true if the entity can use the controller, and false if it cannot.</returns>
    private bool PlayerCanUseController(Entity<AmeControllerComponent?> controller, EntityUid playerEntity, bool needsPower = true)
    {
        if (!Resolve(controller, ref controller.Comp))
            return false;

        //Need player entity to check if they are still able to use the dispenser
        if (!Exists(playerEntity))
            return false;

        //Check if device is powered
        if (needsPower && TryComp<ApcPowerReceiverComponent>(controller, out var powerSource) && !powerSource.Powered)
            return false;

        return true;
    }


    private void OnAddedToGraph(EntityUid uid, AmeControllerComponent comp, ref AddedToGraphEvent args)
    {
        if (!TryComp<AmeComponent>(args.Graph, out var ame))
            return;

        if (ame.MasterController is { })
            return;

        _ameSystem.SetMasterController((args.Graph.Owner, ame), uid);

        UpdateUi((uid, comp));
    }

    private void OnRemovedFromGraph(EntityUid uid, AmeControllerComponent comp, ref RemovedFromGraphEvent args)
    {
        if (!TryComp<AmeComponent>(args.Graph, out var ame))
            return;

        if (uid != ame.MasterController)
            return;

        foreach (var nodeId in args.Graph.Comp.Nodes)
        {
            var hostId = _nodeSystem.GetNodeHost((nodeId, null, null));
            if (!HasComp<AmeControllerComponent>(hostId) || hostId == uid)
                continue;

            _ameSystem.SetMasterController((args.Graph, ame), hostId);
            return;
        }

        _ameSystem.SetMasterController((args.Graph, ame), null);
    }

    private void OnAddedToGraph(EntityUid uid, AmeControllerComponent comp, ref ProxyNodeRelayEvent<AddedToGraphEvent> args)
        => OnAddedToGraph(uid, comp, ref args.Event);

    private void OnRemovedFromGraph(EntityUid uid, AmeControllerComponent comp, ref ProxyNodeRelayEvent<RemovedFromGraphEvent> args)
        => OnRemovedFromGraph(uid, comp, ref args.Event);
}
