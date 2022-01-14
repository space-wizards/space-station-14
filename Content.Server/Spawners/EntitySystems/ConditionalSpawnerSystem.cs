using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Spawners.EntitySystems
{
    [UsedImplicitly]
    public class ConditionalSpawnerSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GameRuleStartedEvent>(OnRuleAdded);
        }

        private void OnRuleAdded(GameRuleStartedEvent args)
        {
            foreach (var spawner in EntityManager.EntityQuery<ConditionalSpawnerComponent>())
            {
                spawner.RuleAdded(args);
            }
        }
    }
}
