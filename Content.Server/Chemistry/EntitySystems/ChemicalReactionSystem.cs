using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems
{
    public sealed class ChemicalReactionSystem : SharedChemicalReactionSystem
    {
        protected override void OnReaction(Solution solution, ReactionPrototype reaction, ReagentPrototype randomReagent, EntityUid owner, FixedPoint2 unitReactions)
        {
            base.OnReaction(solution, reaction,  randomReagent, owner, unitReactions);

            var coordinates = Transform(owner).Coordinates;

            AdminLogger.Add(LogType.ChemicalReaction, reaction.Impact,
                $"Chemical reaction {reaction.ID:reaction} occurred with strength {unitReactions:strength} on entity {ToPrettyString(owner):metabolizer} at {coordinates}");

            SoundSystem.Play(reaction.Sound.GetSound(), Filter.Pvs(owner, entityManager:EntityManager), owner);
        }
    }
}
