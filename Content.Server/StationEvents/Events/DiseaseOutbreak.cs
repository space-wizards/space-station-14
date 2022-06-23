using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Disease.Components;
using Content.Server.Disease;
using Content.Server.Station.Systems;
using Content.Shared.Disease;
using Content.Shared.MobState.Components;
using Content.Shared.Sound;
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

    /// <summary>
    /// Disease prototypes I decided were not too deadly for a random event
    /// </summary>
    public readonly IReadOnlyList<string> NotTooSeriousDiseases = new[]
    {
        "SpaceCold",
        "VanAusdallsRobovirus",
        "VentCough",
        "AMIV",
        "SpaceFlu",
        "BirdFlew"
    };
    public override string Name => "DiseaseOutbreak";
    public override float Weight => WeightNormal;

    public override SoundSpecifier? StartAudio => new SoundPathSpecifier("/Audio/Announcements/outbreak7.ogg");
    protected override float EndAfter => 1.0f;

    public override bool AnnounceEvent => false;

    /// <summary>
    /// Finds 2-5 random, alive entities that can host diseases
    /// and gives them a randomly selected disease.
    /// They all get the same disease.
    /// </summary>
    public override void Startup()
    {
        base.Startup();
        HashSet<EntityUid> stationsToNotify = new();
        List<DiseaseCarrierComponent> aliveList = new();
        foreach (var (carrier, mobState) in _entityManager.EntityQuery<DiseaseCarrierComponent, MobStateComponent>())
        {
            if (!mobState.IsDead())
                aliveList.Add(carrier);
        }
        _random.Shuffle(aliveList);
        /// We're going to filter the above out to only alive mobs. Might change after future mobstate rework

        var toInfect = _random.Next(2, 5);

        var diseaseName = _random.Pick(NotTooSeriousDiseases);

        if (!_prototypeManager.TryIndex(diseaseName, out DiseasePrototype? disease))
            return;

        var diseaseSystem = EntitySystem.Get<DiseaseSystem>();
        var entSysMgr = IoCManager.Resolve<IEntitySystemManager>();
        var stationSystem = entSysMgr.GetEntitySystem<StationSystem>();
        var chatSystem = entSysMgr.GetEntitySystem<ChatSystem>();
        // Now we give it to people in the list of living disease carriers earlier
        foreach (var target in aliveList)
        {
            if (toInfect-- == 0)
                break;

            diseaseSystem.TryAddDisease(target.Owner, disease, target);

            var station = stationSystem.GetOwningStation(target.Owner);
            if(station == null) continue;
            stationsToNotify.Add((EntityUid) station);
        }

        if (!AnnounceEvent)
            return;
        foreach (var station in stationsToNotify)
        {
            chatSystem.DispatchStationAnnouncement(station, Loc.GetString("station-event-disease-outbreak-announcement"),
                playDefaultSound: false, colorOverride: Color.YellowGreen);
        }
    }
}
