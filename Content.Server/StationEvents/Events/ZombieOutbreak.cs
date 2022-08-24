using Content.Shared.MobState.Components;
using Content.Server.Zombies;

namespace Content.Server.StationEvents.Events
{
    /// <summary>
    /// Revives several dead entities as zombies
    /// </summary>
    public sealed class ZombieOutbreak : StationEventSystem
    {
        [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;

        public override string Prototype => "ZombieOutbreak";

        /// <summary>
        /// Finds 1-3 random, dead entities across the station
        /// and turns them into zombies.
        /// </summary>
        public override void Started()
        {
            base.Started();
            List<MobStateComponent> deadList = new();
            foreach (var mobState in EntityManager.EntityQuery<MobStateComponent>())
            {
                if (mobState.IsDead() || mobState.IsCritical())
                    deadList.Add(mobState);
            }
            RobustRandom.Shuffle(deadList);

            var toInfect = RobustRandom.Next(1, 3);

            foreach (var target in deadList)
            {
                if (toInfect-- == 0)
                    break;

                _zombify.ZombifyEntity(target.Owner);
            }
        }
    }
}
