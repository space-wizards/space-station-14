using Content.Server.Cargo.Components;
using Content.Server.Station.Systems;
using Content.Shared.Cargo;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem : SharedCargoSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("cargo");
        InitializeConsole();
        InitializeShuttle();
        InitializeTelepad();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownShuttle();
        CleanupShuttle();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateConsole(frameTime);
        UpdateTelepad(frameTime);
    }

    [PublicAPI]
    public void UpdateBankAccount(StationBankAccountComponent component, int balanceAdded)
    {
        component.Balance += balanceAdded;
        // TODO: Code bad
        foreach (var comp in EntityQuery<CargoOrderConsoleComponent>())
        {
            if (!_uiSystem.IsUiOpen(comp.Owner, CargoConsoleUiKey.Orders)) continue;

            var station = _station.GetOwningStation(comp.Owner);
            if (station != component.Owner)
                continue;

            UpdateOrderState(comp, station);
        }
    }
}
