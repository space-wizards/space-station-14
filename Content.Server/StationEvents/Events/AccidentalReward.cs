using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class AccidentalReward : StationEventSystem
{
    [Dependency] private readonly CargoSystem _cargoSystem = default!;
    public override string Prototype => "AccidentalReward";
    private int _balance = 0;

    public override void Started()
    {
        base.Started();

        _balance = RobustRandom.Next(1000, 10000);
        var banks = EntityQuery<StationBankAccountComponent>(true).ToList();

        foreach (var bank in banks)
        {
            _cargoSystem.UpdateBankAccount(bank, _balance);
        }
    }

    public override void Ended()
    {
        var banks = EntityQuery<StationBankAccountComponent>(true).ToList();

        foreach (var bank in banks)
        {
            _cargoSystem.UpdateBankAccount(bank, -_balance);
        }

        var str = Loc.GetString("accidental-reward-event-announcement", ("balance", _balance));
        ChatSystem.DispatchGlobalAnnouncement(str);
    }
}
