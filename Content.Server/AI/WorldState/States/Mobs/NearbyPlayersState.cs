using System.Collections.Generic;
using System.Linq;
using Content.Server.AI.Components;
using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.AI.WorldState.States.Mobs
{
    [UsedImplicitly]
    public sealed class NearbyPlayersState : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyPlayers";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out AiControllerComponent? controller))
            {
                return result;
            }

            var nearbyPlayers = Filter.Empty()
                .AddInRange(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).MapPosition, controller.VisionRadius)
                .Recipients;

            foreach (var player in nearbyPlayers)
            {
                if (player.AttachedEntity == null)
                {
                    continue;
                }

                if (player.AttachedEntity != Owner && IoCManager.Resolve<IEntityManager>().HasComponent<DamageableComponent>(player.AttachedEntity))
                {
                    result.Add(player.AttachedEntity);
                }
            }

            return result;
        }
    }
}
