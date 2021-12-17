using System;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Presets
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    [Prototype("gamePreset")]
    public class GamePresetPrototype : IPrototype
    {
        [DataField("id", required:true)]
        public string ID { get; } = default!;

        [DataField("name")]
        public string ModeTitle { get; } = "????";

        [DataField("description")]
        public string Description { get; } = string.Empty;

        [DataField("rules", customTypeSerializer:typeof(PrototypeIdListSerializer<GameRulePrototype>))]
        public string[] Rules { get; } = Array.Empty<string>();

        /*
        /// <summary>
        /// Called when a player attempts to ghost.
        /// </summary>
        public virtual bool OnGhostAttempt(Mind.Mind mind, bool canReturnGlobal)
        {
            var playerEntity = mind.OwnedEntity;

            var entities = IoCManager.Resolve<IEntityManager>();
            if (entities.HasComponent<GhostComponent>(playerEntity))
                return false;

            if (mind.VisitingEntity != default)
            {
                mind.UnVisit();
            }

            var position = playerEntity is {Valid: true}
                ? _entities.GetComponent<TransformComponent>(playerEntity.Value).Coordinates
                : EntitySystem.Get<GameTicker>().GetObserverSpawnPoint();
            // Ok, so, this is the master place for the logic for if ghosting is "too cheaty" to allow returning.
            // There's no reason at this time to move it to any other place, especially given that the 'side effects required' situations would also have to be moved.
            // + If CharacterDeadPhysically applies, we're physically dead. Therefore, ghosting OK, and we can return (this is critical for gibbing)
            //   Note that we could theoretically be ICly dead and still physically alive and vice versa.
            //   (For example, a zombie could be dead ICly, but may retain memories and is definitely physically active)
            // + If we're in a mob that is critical, and we're supposed to be able to return if possible,
            ///   we're succumbing - the mob is killed. Therefore, character is dead. Ghosting OK.
            //   (If the mob survives, that's a bug. Ghosting is kept regardless.)
            var canReturn = canReturnGlobal && mind.CharacterDeadPhysically;

            if (canReturnGlobal && entities.TryGetComponent(playerEntity, out MobStateComponent? mobState))
            {
                if (mobState.IsCritical())
                {
                    canReturn = true;

                    //todo: what if they dont breathe lol
                    //cry deeply
                    DamageSpecifier damage = new(IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>("Asphyxiation"), 200);
                    EntitySystem.Get<DamageableSystem>().TryChangeDamage(playerEntity, damage, true);
                }
            }

            var ghost = entities.SpawnEntity("MobObserver", position.ToMap(entities));

            // Try setting the ghost entity name to either the character name or the player name.
            // If all else fails, it'll default to the default entity prototype name, "observer".
            // However, that should rarely happen.
            if(!string.IsNullOrWhiteSpace(mind.CharacterName))
                entities.GetComponent<MetaDataComponent>(ghost).EntityName = mind.CharacterName;
            else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                entities.GetComponent<MetaDataComponent>(ghost).EntityName = mind.Session.Name;

            var ghostComponent = entities.GetComponent<GhostComponent>(ghost);

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
        }*/
    }
}
