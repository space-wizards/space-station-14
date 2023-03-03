using System.Linq;
using Content.Server.Fax;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.GameTicking;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Craft.StationGoals;
public sealed class StationGoalPaperSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;

    public List<CargoProductPrototype> advancedPrototypes = new();
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        advancedPrototypes.Clear();
        SendRandomGoal();
    }

    private void OnRoundEnded(RoundEndedEvent ev)
    {
        advancedPrototypes.Clear();
    }

    public bool SendRandomGoal()
    {
        var availableGoals = _prototypeManager.EnumeratePrototypes<StationGoalPrototype>().ToList();
        var goal = _random.Pick(availableGoals);

        return SendStationGoal(goal);
    }

    private void AddAdvancedProductsToCargo(List<string> cargoAdvancedProductsIDs)
    {
        advancedPrototypes.Clear();
        var prototypes = _prototypeManager.EnumeratePrototypes<CargoProductPrototype>();
        var filteredPrototypes = prototypes
            .ToList()
            .FindAll(prototype => cargoAdvancedProductsIDs.Contains(prototype.ID));

        filteredPrototypes.ForEach(prototype =>
        {
            //Если вдруг кто-то добавит по-умолчанию включенный прототип
            if (!prototype.Enabled)
            {
                advancedPrototypes.Add(prototype);
            }
        });
    }

    public bool SendStationGoal(StationGoalPrototype goal)
    {
        var faxes = EntityManager.EntityQuery<FaxMachineComponent>();
        var wasSent = false;
        foreach (var fax in faxes)
        {
            if (!fax.ReceiveStationGoal) continue;

            var printout = new FaxPrintout(
                Loc.GetString(goal.Text),
                Loc.GetString("station-goal-fax-paper-name"),
                null,
                "paper_stamp-cent",
                new() { Loc.GetString("stamp-component-stamped-name-centcom") });
            _faxSystem.Receive(fax.Owner, printout, null, fax);

            wasSent = true;
        }

        if (wasSent && goal.CargoAdvancedProductsIDs.Count > 0)
        {
            AddAdvancedProductsToCargo(goal.CargoAdvancedProductsIDs);
        }

        return wasSent;
    }
}
