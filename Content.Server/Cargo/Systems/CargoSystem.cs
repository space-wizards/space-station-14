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
        CleanupCargoShuttle();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateConsole(frameTime);
        UpdateTelepad(frameTime);
    }

    [PublicAPI]
    public void UpdateBankAccount(EntityUid uid, StationBankAccountComponent component, int balanceAdded)
    {
        component.Balance += balanceAdded;
        var query = EntityQueryEnumerator<CargoOrderConsoleComponent>();

        while (query.MoveNext(out var oUid, out var oComp))
        {
            if (!_uiSystem.IsUiOpen(oUid, CargoConsoleUiKey.Orders))
                continue;

            var station = _station.GetOwningStation(oUid);
            if (station != uid)
                continue;

            UpdateOrderState(oComp, station);
        }
    }
}
