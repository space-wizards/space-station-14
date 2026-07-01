using Content.Shared.Administration.Logs;
using Content.Shared.Ame.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Power.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Ame.Systems;

public abstract partial class SharedAmeControllerSystem : EntitySystem
{
    [Dependency] private AmeNodeGroupHandler _ameHandler = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] protected SharedUserInterfaceSystem UISystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeControllerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AmeControllerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AmeControllerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<AmeControllerComponent, EntInsertedIntoContainerMessage>(OnItemSlotInserted);
        SubscribeLocalEvent<AmeControllerComponent, EntRemovedFromContainerMessage>(OnItemSlotEjected);
        SubscribeLocalEvent<AmeControllerComponent, PowerChangedEvent>(OnPowerChanged);

        Subs.BuiEvents<AmeControllerComponent>(AmeControllerUiKey.Key,
            subs =>
            {
                subs.Event<AmeControllerEjectMessage>(OnEjectMessage);
                subs.Event<AmeControllerToggleInjectionMessage>(OnToggleInjectionMessage);
                subs.Event<AmeControllerIncreaseFuelMessage>(OnIncreaseFuelMessage);
                subs.Event<AmeControllerDecreaseFuelMessage>(OnDecreaseFuelMessage);
            });
    }

    private void OnInit(Entity<AmeControllerComponent> ent, ref ComponentInit args)
    {
        _itemSlots.AddItemSlot(ent.Owner, AmeControllerComponent.FuelSlotId, ent.Comp.FuelSlot);
    }

    private void OnMapInit(Entity<AmeControllerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUIUpdate = _gameTiming.CurTime;
        UpdateUi(ent.AsNullable());
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<AmeControllerComponent, NodeContainerComponent>();
        while (query.MoveNext(out var uid, out var controller, out var nodes))
        {
            if (controller.NextUpdate <= curTime)
                UpdateController((uid, controller, nodes), curTime);
            else if (controller.NextUIUpdate <= curTime)
                UpdateUi((uid, controller));
        }
    }

    private void OnRemove(Entity<AmeControllerComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent.Owner, ent.Comp.FuelSlot);
    }

    private void OnItemSlotInserted(Entity<AmeControllerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!ent.Comp.Initialized || args.Container.ID != ent.Comp.FuelSlot.ID)
            return;

        UpdateUi(ent.AsNullable());
    }

    private void OnItemSlotEjected(Entity<AmeControllerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!ent.Comp.Initialized || args.Container.ID != ent.Comp.FuelSlot.ID)
            return;

        UpdateUi(ent.AsNullable());
    }

    private void UpdateController(Entity<AmeControllerComponent?, NodeContainerComponent?> ent, TimeSpan curTime)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1))
            return;

        ent.Comp1.LastUpdate = curTime;
        ent.Comp1.NextUpdate = curTime + ent.Comp1.UpdatePeriod;
        // update the UI regardless of other factors to update the power readings
        UpdateUi((ent.Owner, ent.Comp1));

        if (!ent.Comp1.Injecting)
            return;

        if (!_nodeContainer.TryGetFirstNodeGroup<AmeNodeGroup>((ent.Owner, ent.Comp2), out var group))
            return;

        if (TryComp<AmeFuelContainerComponent>(ent.Comp1.FuelSlot.Item, out var fuelContainer))
        {
            // if the jar is empty shut down the AME
            if (fuelContainer.FuelAmount <= 0)
            {
                SetInjecting(ent, false);
            }
            else
            {
                var availableInject = Math.Min(ent.Comp1.InjectionAmount, fuelContainer.FuelAmount);
                var powerOutput = _ameHandler.InjectFuel(group, availableInject, out var overloading);
                if (TryComp<PowerSupplierComponent>(ent, out var powerOutlet))
                    powerOutlet.MaxSupply = powerOutput;

                fuelContainer.FuelAmount -= availableInject;

                // Dirty for the sake of the AME fuel examine not mispredicting
                Dirty(ent.Comp1.FuelSlot.Item.Value, fuelContainer);

                // only play audio if we actually had an injection
                if (availableInject > 0)
                    _audioSystem.PlayPvs(ent.Comp1.InjectSound, ent, AudioParams.Default.WithVolume(overloading ? 10f : 0f));
                UpdateUi(ent);
            }
        }

        ent.Comp1.Stability = _ameHandler.GetTotalStability(group);

        _ameHandler.UpdateCoreVisuals(group);
        UpdateDisplay((ent.Owner, ent.Comp1), ent.Comp1.Stability);

        if (ent.Comp1.Stability <= 0)
            _ameHandler.ExplodeCores(group);
    }

    public abstract void UpdateUi(Entity<AmeControllerComponent?> ent);

    public void UpdateVisuals(Entity<AmeControllerComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (_nodeContainer.TryGetFirstNodeGroup<AmeNodeGroup>(ent.Owner, out var group))
            _ameHandler.UpdateCoreVisuals(group);
    }

    public void TryEject(Entity<AmeControllerComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Injecting)
            return;

        if (!Exists(ent.Comp.FuelSlot.Item))
            return;

        _itemSlots.TryEjectToHands(ent, ent.Comp.FuelSlot, user);

        UpdateUi(ent);
    }

    public void SetInjecting(Entity<AmeControllerComponent?> ent, bool value, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Injecting == value)
            return;

        ent.Comp.Injecting = value;
        UpdateDisplay(ent, ent.Comp.Stability);
        if (!value && TryComp<PowerSupplierComponent>(ent, out var powerOut))
            powerOut.MaxSupply = 0;

        UpdateUi(ent);

        _itemSlots.SetLock(ent, ent.Comp.FuelSlot, value);

        // Logging
        if (!HasComp<MindContainerComponent>(user))
            return;

        var humanReadableState = value ? "Inject" : "Not inject";
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user.Value):player} has set the AME to {humanReadableState}");
    }

    public void ToggleInjecting(Entity<AmeControllerComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        SetInjecting(ent, !ent.Comp.Injecting, user);
    }

    public void SetInjectionAmount(Entity<AmeControllerComponent?> ent, int value, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;
        if (ent.Comp.InjectionAmount == value)
            return;

        var oldValue = ent.Comp.InjectionAmount;
        ent.Comp.InjectionAmount = value;

        UpdateUi(ent);

        // Logging
        if (!HasComp<MindContainerComponent>(user))
            return;

        var humanReadableState = ent.Comp.Injecting ? "Inject" : "Not inject";

        var safeLimit = int.MaxValue;
        if (_nodeContainer.TryGetFirstNodeGroup<AmeNodeGroup>(ent.Owner, out var group))
            safeLimit = group.Cores.Count * 4;

        var logImpact = (oldValue <= safeLimit && value > safeLimit) ? LogImpact.Extreme : LogImpact.Medium;

        _adminLogger.Add(LogType.Action,
            logImpact,
            $"{ToPrettyString(user.Value):player} has set the AME to inject {ent.Comp.InjectionAmount} while set to {humanReadableState}");
    }

    public void AdjustInjectionAmount(Entity<AmeControllerComponent?> ent, int delta, EntityUid? user = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var max = GetMaxInjectionAmount(ent!);
        SetInjectionAmount(ent.Owner, MathHelper.Clamp(ent.Comp.InjectionAmount + delta, 0, max), user);
    }

    public int GetMaxInjectionAmount(Entity<AmeControllerComponent> ent)
    {
        if (!_nodeContainer.TryGetFirstNodeGroup<AmeNodeGroup>(ent.Owner, out var group))
            return 0;

        return  group.Cores.Count * 8;
    }

    private void UpdateDisplay(Entity<AmeControllerComponent?, AppearanceComponent?> ent, int stability)
    {
        if (!Resolve(ent.Owner, ref ent.Comp1, ref ent.Comp2))
            return;

        var ameControllerState = stability switch
        {
            < 10 => AmeControllerState.Fuck,
            < 50 => AmeControllerState.Critical,
            < 80 => AmeControllerState.Warning,
            _ => AmeControllerState.On,
        };

        if (!ent.Comp1.Injecting)
            ameControllerState = AmeControllerState.Off;

        _appearanceSystem.SetData(
            ent,
            AmeControllerVisuals.DisplayState,
            ameControllerState,
            ent.Comp2
        );
    }

    private void OnPowerChanged(Entity<AmeControllerComponent> ent, ref PowerChangedEvent args)
    {
        UpdateUi(ent.AsNullable());
    }

    private void OnEjectMessage(Entity<AmeControllerComponent> control, ref AmeControllerEjectMessage msg)
    {
        var ent = control.AsNullable();
        var user = msg.Actor;

        if (!PlayerCanUseController(ent, user, false))
            return;

        TryEject(ent, user);

        _audioSystem.PlayPredicted(control.Comp.ClickSound, ent, user, AudioParams.Default.WithVolume(-2f));

        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    private void OnToggleInjectionMessage(Entity<AmeControllerComponent> control, ref AmeControllerToggleInjectionMessage msg)
    {
        var ent = control.AsNullable();
        var user = msg.Actor;

        if (!PlayerCanUseController(ent, user, false))
            return;

        ToggleInjecting(ent, user);

        _audioSystem.PlayPredicted(control.Comp.ClickSound, ent, user, AudioParams.Default.WithVolume(-2f));

        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    private void OnIncreaseFuelMessage(Entity<AmeControllerComponent> control, ref AmeControllerIncreaseFuelMessage msg)
    {
        var ent = control.AsNullable();
        var user = msg.Actor;

        if (!PlayerCanUseController(ent, user, false))
            return;

        AdjustInjectionAmount(ent, +2, user);

        _audioSystem.PlayPredicted(control.Comp.ClickSound, ent, user, AudioParams.Default.WithVolume(-2f));

        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    private void OnDecreaseFuelMessage(Entity<AmeControllerComponent> control, ref AmeControllerDecreaseFuelMessage msg)
    {
        var ent = control.AsNullable();
        var user = msg.Actor;

        if (!PlayerCanUseController(ent, user, false))
            return;

        AdjustInjectionAmount(ent, -2, user);

        _audioSystem.PlayPredicted(control.Comp.ClickSound, ent, user, AudioParams.Default.WithVolume(-2f));

        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    /// <summary>
    /// Checks whether the player entity is able to use the controller.
    /// </summary>
    /// <returns>Returns true if the entity can use the controller, and false if it cannot.</returns>
    private bool PlayerCanUseController(Entity<AmeControllerComponent?> ent, EntityUid playerEntity, bool needsPower = true)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        //Need player entity to check if they are still able to use the dispenser
        if (!Exists(playerEntity))
            return false;

        //Check if device is powered
        if (needsPower && TryComp<PowerReceiverComponent>(ent, out var powerSource) && !powerSource.Powered)
            return false;

        return true;
    }
}
