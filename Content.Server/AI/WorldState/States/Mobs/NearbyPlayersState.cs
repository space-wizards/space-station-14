using System.Collections.Generic;
using Content.Server.AI.Components;
using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Server.AI.WorldState.States.Mobs
{
    [UsedImplicitly]
    public sealed class NearbyPlayersState : CachedStateData<List<EntityUid>>
    {
        public override string Name => "NearbyPlayers";

        protected override List<EntityUid> GetTrueValue()
        {
            var result = new List<EntityUid>();

            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(Owner, out AiControllerComponent? controller))
            {
                return result;
            }

            var nearbyPlayers = Filter.Empty()
                .AddInRange(entMan.GetComponent<TransformComponent>(Owner).MapPosition, controller.VisionRadius)
                .Recipients;

            foreach (var player in nearbyPlayers)
            {
                if (player.AttachedEntity is not {Valid: true} playerEntity)
                {
                    continue;
                }

                if (player.AttachedEntity != Owner && entMan.HasComponent<DamageableComponent>(playerEntity))
                {
                    result.Add(playerEntity);
                }
            }

            return result;
        }
    }
}
