using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Ame.Components;
using Content.Server.Chat.Managers;
using Content.Server.NodeContainer;
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
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeControllerComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AmeControllerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AmeControllerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AmeControllerComponent, UiButtonPressedMessage>(OnUiButtonPressed);
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<AmeControllerComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var controller, out var nodes))
        {
            if (controller.NextUpdate <= curTime)
                UpdateController(uid, curTime, controller, nodes);
        }
    }

    private void UpdateController(EntityUid uid, TimeSpan curTime, AmeControllerComponent? controller = null, NodeContainerComponent? nodes = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        controller.LastUpdate = curTime;
        controller.NextUpdate = curTime + controller.UpdatePeriod;

        if (!controller.Injecting)
            return;
        if (!TryGetAMENodeGroup(uid, out var group, nodes))
            return;

        if (TryComp<AmeFuelContainerComponent>(controller.JarSlot.ContainedEntity, out var fuelJar))
        {
            var availableInject = Math.Min(controller.InjectionAmount, fuelJar.FuelAmount);
            var powerOutput = group.InjectFuel(availableInject, out var overloading);
            if (TryComp<PowerSupplierComponent>(uid, out var powerOutlet))
                powerOutlet.MaxSupply = powerOutput;
            fuelJar.FuelAmount -= availableInject;
            _audioSystem.PlayPvs(controller.InjectSound, uid, AudioParams.Default.WithVolume(overloading ? 10f : 0f));
            UpdateUi(uid, controller);
        }

        controller.Stability = group.GetTotalStability();

        UpdateDisplay(uid, controller.Stability, controller);

        if (controller.Stability <= 0)
            group.ExplodeCores();
    }

    public void UpdateUi(EntityUid uid, AmeControllerComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        if (!_userInterfaceSystem.TryGetUi(uid, AmeControllerUiKey.Key, out var bui))
            return;

        var state = GetUiState(uid, controller);
        _userInterfaceSystem.SetUiState(bui, state);
    }

    private AmeControllerBoundUserInterfaceState GetUiState(EntityUid uid, AmeControllerComponent controller)
    {
        var powered = !TryComp<ApcPowerReceiverComponent>(uid, out var powerSource) || powerSource.Powered;
        var coreCount = TryGetAMENodeGroup(uid, out var group) ? group.CoreCount : 0;

        var hasJar = Exists(controller.JarSlot.ContainedEntity);
        if (!hasJar || !TryComp<AmeFuelContainerComponent>(controller.JarSlot.ContainedEntity, out var jar))
            return new AmeControllerBoundUserInterfaceState(powered, IsMasterController(uid), false, hasJar, 0, controller.InjectionAmount, coreCount);

        return new AmeControllerBoundUserInterfaceState(powered, IsMasterController(uid), controller.Injecting, hasJar, jar.FuelAmount, controller.InjectionAmount, coreCount);
    }

    private bool IsMasterController(EntityUid uid)
    {
        return TryGetAMENodeGroup(uid, out var group) && group.MasterController == uid;
    }

    private bool TryGetAMENodeGroup(EntityUid uid, [MaybeNullWhen(false)] out AmeNodeGroup group, NodeContainerComponent? nodes = null)
    {
        if (!Resolve(uid, ref nodes))
        {
            group = null;
            return false;
        }

        group = nodes.Nodes.Values
            .Select(node => node.NodeGroup)
            .OfType<AmeNodeGroup>()
            .FirstOrDefault();

        return group != null;
    }

    public void TryEject(EntityUid uid, EntityUid? user = null, AmeControllerComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;
        if (controller.Injecting)
            return;

        var jar = controller.JarSlot.ContainedEntity;
        if (!Exists(jar))
            return;

        controller.JarSlot.Remove(jar!.Value);
        UpdateUi(uid, controller);
        if (Exists(user))
            _handsSystem.PickupOrDrop(user, jar!.Value);
    }

    public void SetInjecting(EntityUid uid, bool value, EntityUid? user = null, AmeControllerComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;
        if (controller.Injecting == value)
            return;

        controller.Injecting = value;
        _appearanceSystem.SetData(uid, AmeControllerVisuals.DisplayState, value ? AmeControllerState.On : AmeControllerState.Off);
        if (!value && TryComp<PowerSupplierComponent>(uid, out var powerOut))
            powerOut.MaxSupply = 0;

        UpdateUi(uid, controller);

        // Logging
        if (!HasComp<MindContainerComponent>(user))
            return;

        var humanReadableState = value ? "Inject" : "Not inject";
        _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{EntityManager.ToPrettyString(user.Value):player} has set the AME to {humanReadableState}");
    }

    public void ToggleInjecting(EntityUid uid, EntityUid? user = null, AmeControllerComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;
        SetInjecting(uid, !controller.Injecting, user, controller);
    }

    public void SetInjectionAmount(EntityUid uid, int value, EntityUid? user = null, AmeControllerComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;
        if (controller.InjectionAmount == value)
            return;

        var oldValue = controller.InjectionAmount;
        controller.InjectionAmount = value;

        UpdateUi(uid, controller);

        // Logging
        if (!TryComp<MindContainerComponent>(user, out var mindContainer))
            return;

        var humanReadableState = controller.Injecting ? "Inject" : "Not inject";
        _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{EntityManager.ToPrettyString(user.Value):player} has set the AME to inject {controller.InjectionAmount} while set to {humanReadableState}");

        // Admin alert
        var safeLimit = 0;
        if (TryGetAMENodeGroup(uid, out var group))
            safeLimit = group.CoreCount * 2;

        if (oldValue <= safeLimit && value > safeLimit)
            _chatManager.SendAdminAlert(user.Value, $"increased AME over safe limit to {controller.InjectionAmount}");
    }

    public void AdjustInjectionAmount(EntityUid uid, int delta, int min = 0, int max = int.MaxValue, EntityUid? user = null, AmeControllerComponent? controller = null)
    {
        if (Resolve(uid, ref controller))
            SetInjectionAmount(uid, MathHelper.Clamp(controller.InjectionAmount + delta, min, max), user, controller);
    }

    private void UpdateDisplay(EntityUid uid, int stability, AmeControllerComponent? controller = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref controller, ref appearance))
            return;

        _appearanceSystem.SetData(
            uid,
            AmeControllerVisuals.DisplayState,
            stability switch
            {
                < 10 => AmeControllerState.Fuck,
                < 50 => AmeControllerState.Critical,
                _ => AmeControllerState.On,
            },
            appearance
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

        UpdateUi(uid, comp);
    }

    private void OnPowerChanged(EntityUid uid, AmeControllerComponent comp, ref PowerChangedEvent args)
    {
        UpdateUi(uid, comp);
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

        if (!PlayerCanUseController(uid, user!.Value, needsPower, comp))
            return;

        _audioSystem.PlayPvs(comp.ClickSound, uid, AudioParams.Default.WithVolume(-2f));
        switch (msg.Button)
        {
            case UiButton.Eject:
                TryEject(uid, user: user, controller: comp);
                break;
            case UiButton.ToggleInjection:
                ToggleInjecting(uid, user: user, controller: comp);
                break;
            case UiButton.IncreaseFuel:
                AdjustInjectionAmount(uid, +2, user: user, controller: comp);
                break;
            case UiButton.DecreaseFuel:
                AdjustInjectionAmount(uid, -2, user: user, controller: comp);
                break;
        }

        if (TryGetAMENodeGroup(uid, out var group))
            group.UpdateCoreVisuals();

        UpdateUi(uid, comp);
    }

    /// <summary>
    /// Checks whether the player entity is able to use the controller.
    /// </summary>
    /// <param name="playerEntity">The player entity.</param>
    /// <returns>Returns true if the entity can use the controller, and false if it cannot.</returns>
    private bool PlayerCanUseController(EntityUid uid, EntityUid playerEntity, bool needsPower = true, AmeControllerComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return false;

        //Need player entity to check if they are still able to use the dispenser
        if (!Exists(playerEntity))
            return false;

        //Check if device is powered
        if (needsPower && TryComp<ApcPowerReceiverComponent>(uid, out var powerSource) && !powerSource.Powered)
            return false;

        return true;
    }
}
