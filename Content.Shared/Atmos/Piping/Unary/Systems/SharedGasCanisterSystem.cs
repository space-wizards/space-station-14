using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.NodeContainer;
using Robust.Shared.Containers;
using GasCanisterComponent = Content.Shared.Atmos.Piping.Unary.Components.GasCanisterComponent;

namespace Content.Shared.Atmos.Piping.Unary.Systems;

public abstract class SharedGasCanisterSystem : EntitySystem
{
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;
    [Dependency] private   readonly ItemSlotsSystem _slots = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasCanisterComponent, EntInsertedIntoContainerMessage>(OnCanisterContainerModified);
        SubscribeLocalEvent<GasCanisterComponent, EntRemovedFromContainerMessage>(OnCanisterContainerModified);
        SubscribeLocalEvent<GasCanisterComponent, ItemSlotInsertAttemptEvent>(OnCanisterInsertAttempt);
        SubscribeLocalEvent<GasCanisterComponent, ComponentStartup>(OnCanisterStartup);

        // Bound UI subscriptions
        SubscribeLocalEvent<GasCanisterComponent, GasCanisterHoldingTankEjectMessage>(OnHoldingTankEjectMessage);
        SubscribeLocalEvent<GasCanisterComponent, GasCanisterChangeReleasePressureMessage>(OnCanisterChangeReleasePressure);
        SubscribeLocalEvent<GasCanisterComponent, GasCanisterChangeReleaseValveMessage>(OnCanisterChangeReleaseValve);
    }

    private void OnCanisterStartup(Entity<GasCanisterComponent> ent, ref ComponentStartup args)
    {
        // Ensure container
        _slots.AddItemSlot(ent.Owner, ent.Comp.ContainerName, ent.Comp.GasTankSlot);
    }

    private void OnCanisterContainerModified(EntityUid uid, GasCanisterComponent component, ContainerModifiedMessage args)
    {
        if (args.Container.ID != component.ContainerName)
            return;

        DirtyUI(uid, component);
        _appearance.SetData(uid, GasCanisterVisuals.TankInserted, args is EntInsertedIntoContainerMessage);
    }

    private static string GetContainedGasesString(Entity<GasCanisterComponent> canister)
    {
        return string.Join(", ", canister.Comp.Air);
    }

    private void OnHoldingTankEjectMessage(EntityUid uid, GasCanisterComponent canister, GasCanisterHoldingTankEjectMessage args)
    {
        if (canister.GasTankSlot.Item == null)
            return;

        var item = canister.GasTankSlot.Item;
        _slots.TryEjectToHands(uid, canister.GasTankSlot, args.Actor, excludeUserAudio: true);

        if (canister.ReleaseValve)
        {
            AdminLogger.Add(LogType.CanisterTankEjected, LogImpact.High, $"Player {ToPrettyString(args.Actor):player} ejected tank {ToPrettyString(item):tank} from {ToPrettyString(uid):canister} while the valve was open, releasing [{GetContainedGasesString((uid, canister))}] to atmosphere");
        }
        else
        {
            AdminLogger.Add(LogType.CanisterTankEjected, LogImpact.Medium, $"Player {ToPrettyString(args.Actor):player} ejected tank {ToPrettyString(item):tank} from {ToPrettyString(uid):canister}");
        }

        if (UI.TryGetUiState<GasCanisterBoundUserInterfaceState>(uid, GasCanisterUiKey.Key, out var lastState))
        {
            // We can at least predict 0 pressure for now even without atmos prediction.
            var newState = new GasCanisterBoundUserInterfaceState(lastState.CanisterPressure, lastState.PortStatus, 0f);
            UI.SetUiState(uid, GasCanisterUiKey.Key, newState);
        }

        DirtyUI(uid, canister);
    }

    private void OnCanisterChangeReleasePressure(EntityUid uid, GasCanisterComponent canister, GasCanisterChangeReleasePressureMessage args)
    {
        var pressure = Math.Clamp(args.Pressure, canister.MinReleasePressure, canister.MaxReleasePressure);

        AdminLogger.Add(LogType.CanisterPressure, LogImpact.Medium, $"{ToPrettyString(args.Actor):player} set the release pressure on {ToPrettyString(uid):canister} to {args.Pressure}");

        canister.ReleasePressure = pressure;
        Dirty(uid, canister);
        DirtyUI(uid, canister);
    }

    private void OnCanisterChangeReleaseValve(EntityUid uid, GasCanisterComponent canister, GasCanisterChangeReleaseValveMessage args)
    {
        // filling a jetpack with plasma is less important than filling a room with it
        var impact = canister.GasTankSlot.HasItem ? LogImpact.Medium : LogImpact.High;

        var containedGasDict = new Dictionary<Gas, float>();
        var containedGasArray = Enum.GetValues(typeof(Gas));

        for (var i = 0; i < containedGasArray.Length; i++)
        {
            containedGasDict.Add((Gas)i, canister.Air[i]);
        }

        AdminLogger.Add(LogType.CanisterValve, impact, $"{ToPrettyString(args.Actor):player} set the valve on {ToPrettyString(uid):canister} to {args.Valve:valveState} while it contained [{string.Join(", ", containedGasDict)}]");

        canister.ReleaseValve = args.Valve;
        Dirty(uid, canister);
        DirtyUI(uid, canister);
    }

    private void OnCanisterInsertAttempt(EntityUid uid, GasCanisterComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Slot.ID != component.ContainerName || args.User == null)
            return;

        // Could whitelist but we want to check if it's open so.
        if (!TryComp<GasTankComponent>(args.Item, out var gasTank) || gasTank.IsValveOpen)
        {
            args.Cancelled = true;
        }
    }

    protected abstract void DirtyUI(EntityUid uid, GasCanisterComponent? component = null, NodeContainerComponent? nodes = null);
}
