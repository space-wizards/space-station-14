using Robust.Shared.Random;
using Content.Server.Chat.Managers;
using Content.Server.Disease.Zombie.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Sound;

namespace Content.Server.StationEvents.Events
{
    /// <summary>
    /// Revives several dead entities as zombies
    /// </summary>
    public sealed class ZombieOutbreak : StationEvent
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;

        public override string Name => "ZombieOutbreak";
        public override int EarliestStart => 50;
        public override float Weight => WeightLow / 2;

        public override SoundSpecifier? StartAudio => new SoundPathSpecifier("/Audio/Announcements/bloblarm.ogg");
        protected override float EndAfter => 1.0f;

        public override int? MaxOccurrences => 1;

        /// <summary>
        /// Finds 1-3 random, dead entities accross the station
        /// and turns them into zombies.
        /// </summary>
        public override void Startup()
        {
            base.Startup();
            List<MobStateComponent> deadList = new();
            foreach (var mobState in _entityManager.EntityQuery<MobStateComponent>())
            {
                if (mobState.IsDead() || mobState.IsCritical())
                    deadList.Add(mobState);
            }
            _random.Shuffle(deadList);

            var toInfect = _random.Next(1, 3);

            /// Now we give it to people in the list of dead entities earlier.
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
}
