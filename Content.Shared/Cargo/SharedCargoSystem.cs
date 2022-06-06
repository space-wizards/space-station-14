using Content.Shared.Cargo.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

public abstract class SharedCargoSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationBankAccountComponent, ComponentGetState>(OnBankGetState);
        SubscribeLocalEvent<StationBankAccountComponent, ComponentHandleState>(OnBankHandleState);

        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, ComponentGetState>(OnOrderGetState);
        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, ComponentHandleState>(OnOrderHandleState);
    }

    private void OnOrderGetState(EntityUid uid, StationCargoOrderDatabaseComponent component, ref ComponentGetState args)
    {
        args.State = new StationCargoOrderDatabaseComponentState()
        {
            Orders = component.Orders,
        };
    }

    private void OnBankHandleState(EntityUid uid, StationBankAccountComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StationBankAccountComponentState state) return;
        component.Balance = state.Balance;
    }

    private void OnBankGetState(EntityUid uid, StationBankAccountComponent component, ref ComponentGetState args)
    {
        args.State = new StationBankAccountComponentState() { Balance = component.Balance };
    }

    private void OnOrderHandleState(EntityUid uid, StationCargoOrderDatabaseComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StationCargoOrderDatabaseComponentState state) return;
        component.Orders = state.Orders;
        // TODO: Dirty UI.
    }

    [Serializable, NetSerializable]
    protected sealed class StationBankAccountComponentState : ComponentState
    {
        public int Balance;
    }

    [Serializable, NetSerializable]
    protected sealed class StationCargoOrderDatabaseComponentState : ComponentState
    {
        public Dictionary<int, CargoOrderData> Orders = new();
    }
}

[Serializable, NetSerializable]
public enum CargoTelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
};

[Serializable, NetSerializable]
public enum CargoTelepadVisuals : byte
{
    State,
};
