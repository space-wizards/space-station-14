using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Disease.Components;
using Content.Server.Disease;
using Content.Shared.Disease;
using Content.Shared.MobState.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Events;
/// <summary>
/// Revives several dead entities as zombies
/// </summary>
public sealed class ZombieOutbreak : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override string Name => "ZombieOutbreak";
    public override float Weight => WeightLow;

    public override string? StartAudio => "/Audio/Announcements/bloblarm.ogg";
    protected override float EndAfter => 1.0f;

    public override int? MaxOccurrences => 1;

    /// <summary>
    /// Finds 2-5 random, alive entities that can host diseases
    /// and gives them a randomly selected disease.
    /// They all get the same disease.
    /// </summary>
    public override void Startup()
    {
        base.Startup();
        List<MobStateComponent> deadList = new();
        foreach (var mobState in _entityManager.EntityQuery<MobStateComponent>())
        {
            if (mobState.IsDead())
                deadList.Add(mobState);
        }
        _random.Shuffle(deadList);

        var toInfect = _random.Next(5, 7);

        /// Now we give it to people in the list of living disease carriers earlier
        foreach (var target in deadList)
        {
            if (toInfect-- == 0)
                break;

            _entityManager.EnsureComponent<DiseaseZombieComponent>(target.Owner);
        }
        _chatManager.DispatchStationAnnouncement(Loc.GetString("station-event-zombie-outbreak-announcement"),
        playDefaultSound: false, colorOverride: Color.DarkMagenta);
    }
}
