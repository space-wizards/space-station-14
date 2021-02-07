using System.Collections.Generic;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Mobs
{
    [UsedImplicitly]
    public sealed class NearbyPlayersState : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyPlayers";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent controller))
            {
                return result;
            }

            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var nearbyPlayers = playerManager.GetPlayersInRange(Owner.Transform.Coordinates, (int) controller.VisionRadius);

            foreach (var player in nearbyPlayers)
            {
                if (player.AttachedEntity != Owner && player.AttachedEntity.HasComponent<IDamageableComponent>())
                {
                    result.Add(player.AttachedEntity);
                }
            }

            return result;
        }
    }
}
