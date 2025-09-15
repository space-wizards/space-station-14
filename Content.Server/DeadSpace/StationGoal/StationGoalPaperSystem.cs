using System.Linq;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Components;

namespace Content.Server.DeadSpace.StationGoal;

/// <summary>
///     System to spawn paper with station goal
/// </summary>
public sealed class StationGoalPaperSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        var availableGoals = _prototypeManager.EnumeratePrototypes<StationGoalPrototype>().ToList();
        var goal = _random.Pick(availableGoals);

        SendStationGoal(goal);
    }

    /// <summary>
    ///     Send a station goal to all faxes which are authorized to receive it
    /// </summary>
    /// <returns>True if at least one fax received paper</returns>
    public bool SendStationGoal(StationGoalPrototype goal)
    {
        var wasSent = false;
        var wasModifiedOnce = false;

        string text = _resourceManager.ContentFileReadText(goal.Text).ReadToEnd();

        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var uid, out var fax))
        {
            if (!fax.ReceiveStationGoal) continue;

            if (_station.GetOwningStation(uid) is { } station)
            {
                text = text.Replace("STATION XX-00", Name(station));
                if (goal.ModifyStationBalance != null && goal.ModifyStationBalance != 0 && !wasModifiedOnce)
                    wasModifiedOnce = ModifyStationBalance(station, goal.ModifyStationBalance.Value);
            }

            var printout = new FaxPrintout(text, Loc.GetString("station-goal-paper-name"), null, "PaperPrintedCentcomm", "paper_stamp-centcom",
                new List<StampDisplayInfo>
                {
                    new() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") },
                });

            _faxSystem.Receive(uid, printout, null, fax);

            wasSent = true;
        }

        return wasSent;
    }

    /// <summary>
    ///     Adds the amount of money from the prototype of the station goal to the station's cargo balance
    /// </summary>
    /// <returns>True if station balance was modified</returns>
    private bool ModifyStationBalance(EntityUid station, int amount)
    {
        if (!TryComp(station, out StationBankAccountComponent? account))
            return false;

        _cargo.UpdateBankAccount((station, account), amount, account.PrimaryAccount);

        return true;
    }
}
