#nullable enable annotations
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.GameTicking
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    public abstract class GamePreset
    {
        public abstract bool Start(IReadOnlyList<IPlayerSession> readyPlayers, bool force = false);
        public virtual string ModeTitle => "Sandbox";
        public virtual string Description => "Secret!";
        public virtual bool DisallowLateJoin => false;
        public Dictionary<NetUserId, HumanoidCharacterProfile> ReadyProfiles = new();

        public virtual void OnGameStarted() { }

        /// <summary>
        /// Called when a player is spawned in (this includes, but is not limited to, before Start)
        /// </summary>
        public virtual void OnSpawnPlayerCompleted(IPlayerSession session, IEntity mob, bool lateJoin) { }

        /// <summary>
        /// Called when a player attempts to ghost.
        /// </summary>
        public virtual bool OnGhostAttempt(Mind mind, bool canReturnGlobal)
        {
            var playerEntity = mind.OwnedEntity;

            if (playerEntity != null && playerEntity.HasComponent<GhostComponent>())
                return false;

            if (mind.VisitingEntity != null)
            {
                mind.UnVisit();
                mind.VisitingEntity.Delete();
            }

            var position = playerEntity?.Transform.Coordinates ?? IoCManager.Resolve<IGameTicker>().GetObserverSpawnPoint();
            var canReturn = false;

            if (playerEntity != null && canReturnGlobal && playerEntity.TryGetComponent(out IMobStateComponent? mobState))
            {
                if (mobState.IsDead())
                {
                    canReturn = true;
                }
                else if (mobState.IsCritical())
                {
                    canReturn = true;

                    if (playerEntity.TryGetComponent(out IDamageableComponent? damageable))
                    {
                        //todo: what if they dont breathe lol
                        damageable.SetDamage(DamageType.Asphyxiation, 200, playerEntity);
                    }
                }
                else
                {
                    canReturn = false;
                }
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var ghost = entityManager.SpawnEntity("MobObserver", position);
            ghost.Name = mind.CharacterName ?? string.Empty;

            var ghostComponent = ghost.GetComponent<GhostComponent>();
            ghostComponent.CanReturnToBody = canReturn;

            if (canReturn)
                mind.Visit(ghost);
            else
                mind.TransferTo(ghost);
            return true;
        }

        public virtual string GetRoundEndDescription() => "";
    }
}
