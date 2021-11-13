#nullable enable annotations
using System.Collections.Generic;
using Content.Server.Ghost.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Ghost;
using Content.Shared.MobState.Components;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Presets
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
        public virtual bool OnGhostAttempt(Mind.Mind mind, bool canReturnGlobal)
        {
            var playerEntity = mind.OwnedEntity;

            if (playerEntity != null && playerEntity.HasComponent<GhostComponent>())
                return false;

            if (mind.VisitingEntity != null)
            {
                mind.UnVisit();
            }

            var position = playerEntity?.Transform.Coordinates ?? EntitySystem.Get<GameTicker>().GetObserverSpawnPoint();
            // Ok, so, this is the master place for the logic for if ghosting is "too cheaty" to allow returning.
            // There's no reason at this time to move it to any other place, especially given that the 'side effects required' situations would also have to be moved.
            // + If CharacterDeadPhysically applies, we're physically dead. Therefore, ghosting OK, and we can return (this is critical for gibbing)
            //   Note that we could theoretically be ICly dead and still physically alive and vice versa.
            //   (For example, a zombie could be dead ICly, but may retain memories and is definitely physically active)
            // + If we're in a mob that is critical, and we're supposed to be able to return if possible,
            ///   we're succumbing - the mob is killed. Therefore, character is dead. Ghosting OK.
            //   (If the mob survives, that's a bug. Ghosting is kept regardless.)
            var canReturn = canReturnGlobal && mind.CharacterDeadPhysically;

            if (playerEntity != null && canReturnGlobal && playerEntity.TryGetComponent(out MobStateComponent? mobState))
            {
                if (mobState.IsCritical())
                {
                    canReturn = true;

                    //todo: what if they dont breathe lol
                    //cry deeply
                    DamageSpecifier damage = new(IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>("Asphyxiation"), 200);
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(playerEntity.Uid, damage, true);
                }
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var ghost = entityManager.SpawnEntity("MobObserver", position.ToMap(entityManager));

            // Try setting the ghost entity name to either the character name or the player name.
            // If all else fails, it'll default to the default entity prototype name, "observer".
            // However, that should rarely happen.
            if(!string.IsNullOrWhiteSpace(mind.CharacterName))
                ghost.Name = mind.CharacterName;
            else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                ghost.Name = mind.Session.Name;

            var ghostComponent = ghost.GetComponent<GhostComponent>();

            if (mind.TimeOfDeath.HasValue)
            {
                ghostComponent.TimeOfDeath = mind.TimeOfDeath!.Value;
            }

            EntitySystem.Get<SharedGhostSystem>().SetCanReturnToBody(ghostComponent, canReturn);

            if (canReturn)
                mind.Visit(ghost);
            else
                mind.TransferTo(ghost);
            return true;
        }

        public virtual string GetRoundEndDescription() => string.Empty;
    }
}
