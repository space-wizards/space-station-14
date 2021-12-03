using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems
{
    public class ChemicalReactionSystem : SharedChemicalReactionSystem
    {
        protected override void OnReaction(Solution solution, ReactionPrototype reaction, ReagentPrototype randomReagent, EntityUid ownerUid, FixedPoint2 unitReactions)
        {
            base.OnReaction(solution, reaction,  randomReagent, ownerUid, unitReactions);

            var entity = EntityManager.GetEntity(ownerUid);
            _logSystem.Add(LogType.ChemicalReaction, reaction.Impact,
                $"Chemical reaction {reaction.ID} occurred with strength {unitReactions:strength} on entity {entity} at {entity.Transform.Coordinates}");

            SoundSystem.Play(Filter.Pvs(ownerUid, entityManager:EntityManager), reaction.Sound.GetSound(), ownerUid);
        }
    }
}
