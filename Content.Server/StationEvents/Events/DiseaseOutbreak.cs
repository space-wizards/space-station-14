using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Disease.Components;
using Content.Server.Disease;
using Content.Shared.Disease;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Events;

public sealed class DiseaseOutbreak : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    public readonly IReadOnlyList<string> NotTooSeriousDiseases = new[]
    {
        "SpaceCold",
        "VanAusdallsRobovirus"
    };

    public override string Name => "DiseaseOutbreak";

    public override float Weight => WeightNormal;

    protected override float EndAfter => 1.0f;

    public override void Startup()
    {
        base.Startup();

        var targetList = _entityManager.EntityQuery<DiseaseCarrierComponent>().ToList();
        _random.Shuffle(targetList);

        var toInfect = _random.Next(2, 5);

        var diseaseName = _random.Pick(NotTooSeriousDiseases);

        if (!_prototypeManager.TryIndex(diseaseName, out DiseasePrototype? disease) || disease == null)
            return;

        foreach (var target in targetList)
        {
            if (toInfect-- == 0)
                break;

            EntitySystem.Get<DiseaseSystem>().TryAddDisease(target, disease);
        }

        _chatManager.DispatchStationAnnouncement(Loc.GetString("station-event-disease-outbreak-announcement"));
    }
}
