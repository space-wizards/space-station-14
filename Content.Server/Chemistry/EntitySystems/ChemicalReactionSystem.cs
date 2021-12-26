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
        protected override void OnReaction(Solution solution, ReactionPrototype reaction, ReagentPrototype randomReagent, EntityUid Owner, FixedPoint2 unitReactions)
        {
            base.OnReaction(solution, reaction,  randomReagent, Owner, unitReactions);

            var coordinates = Transform(Owner).Coordinates;

            _logSystem.Add(LogType.ChemicalReaction, reaction.Impact,
                $"Chemical reaction {reaction.ID:reaction} occurred with strength {unitReactions:strength} on entity {ToPrettyString(Owner):metabolizer} at {coordinates}");

            SoundSystem.Play(Filter.Pvs(Owner, entityManager:EntityManager), reaction.Sound.GetSound(), Owner);
        }
    }
}
