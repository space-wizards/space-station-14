using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Disease.Components;
using Content.Server.Disease;
using Content.Shared.Disease;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Events;
/// <summary>
/// Infects a couple people
/// with a random disease that isn't super deadly
/// </summary>
public sealed class DiseaseOutbreak : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    /// <summary>
    /// Disease prototypes I decided were not too deadly for a random event
    /// </summary>
    public readonly IReadOnlyList<string> NotTooSeriousDiseases = new[]
    {
        "SpaceCold",
        "VanAusdallsRobovirus",
        "VentCough",
        "AMIV"
    };
    public override string Name => "DiseaseOutbreak";
    public override float Weight => WeightNormal;
    protected override float EndAfter => 1.0f;
    /// <summary>
    /// Finds 2-5 random entities that can host diseases
    /// and gives them a randomly selected disease.
    /// They all get the same disease.
    /// </summary>
    public override void Startup()
    {
        base.Startup();

        var targetList = _entityManager.EntityQuery<DiseaseCarrierComponent>().ToList();
        _random.Shuffle(targetList);

        var toInfect = _random.Next(2, 5);

        var diseaseName = _random.Pick(NotTooSeriousDiseases);

        if (!_prototypeManager.TryIndex(diseaseName, out DiseasePrototype? disease))
            return;

        var diseaseSystem = EntitySystem.Get<DiseaseSystem>();

        foreach (var target in targetList)
        {
            if (toInfect-- == 0)
                break;

            diseaseSystem.TryAddDisease(target.Owner, disease, target);
        }
        _chatManager.DispatchStationAnnouncement(Loc.GetString("station-event-disease-outbreak-announcement"));
    }
}
