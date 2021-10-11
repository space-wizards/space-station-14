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

            SubscribeLocalEvent<GameRuleAddedEvent>(OnRuleAdded);
        }

        private void OnRuleAdded(GameRuleAddedEvent args)
        {
            foreach (var spawner in EntityManager.EntityQuery<ConditionalSpawnerComponent>())
            {
                spawner.RuleAdded(args);
            }
        }
    }
}
