// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.DeadSpace.Virus.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DeadSpace.Virus.Components;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.Virus.Systems;

public sealed class RandomViralSolutionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly VirusSystem _virus = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomViralSolutionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomViralSolutionComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(ent, out var solutionManager))
            return;

        _solution.EnsureAllSolutions((ent.Owner, solutionManager));

        if (!_solution.TryGetSolution((ent.Owner, solutionManager), ent.Comp.Solution, out var solutionEnt, out _))
            return;

        var data = GenerateVirusData(ent.Comp);

        _solution.RemoveAllSolution(solutionEnt.Value);
        _solution.TryAddReagent(
            solutionEnt.Value,
            ent.Comp.Reagent,
            ent.Comp.Quantity,
            out _,
            data: new List<ReagentData> { data });
    }

    private VirusData GenerateVirusData(RandomViralSolutionComponent component)
    {
        var data = new VirusData(_virus.GenerateStrainId());
        data.BodyWhitelist.AddRange(component.BodyWhitelist);
        data.ActiveSymptom.AddRange(PickSymptoms(component));

        return data;
    }

    private List<ProtoId<VirusSymptomPrototype>> PickSymptoms(RandomViralSolutionComponent component)
    {
        var minSymptoms = Math.Max(0, component.MinSymptoms);
        var maxSymptoms = Math.Max(minSymptoms, component.MaxSymptoms);
        var count = _random.Next(minSymptoms, maxSymptoms + 1);
        count = Math.Max(count, component.RequiredDangers.Count);

        var selected = new List<ProtoId<VirusSymptomPrototype>>();
        var available = _prototype
            .EnumeratePrototypes<VirusSymptomPrototype>()
            .Where(proto =>
                component.AllowedDangers.Contains(proto.DangerIndicator) &&
                proto.DangerIndicator != DangerIndicatorSymptom.Cataclysm &&
                !proto.TaipanOnly) // исключаем тайпановские симптомы
            .ToList();

        foreach (var danger in component.RequiredDangers)
        {
            var requiredAvailable = available
                .Where(proto => proto.DangerIndicator == danger)
                .ToList();

            if (requiredAvailable.Count == 0)
                continue;

            var picked = _random.Pick(requiredAvailable);
            selected.Add(picked.ID);
            available.RemoveAll(proto => proto.ID == picked.ID);
        }

        while (selected.Count < count && available.Count > 0)
        {
            selected.Add(_random.PickAndTake(available).ID);
        }

        return selected;
    }
}
