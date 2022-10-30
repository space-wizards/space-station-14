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
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        protected override void OnReaction(Solution solution, ReactionPrototype reaction, ReagentPrototype randomReagent, EntityUid owner, FixedPoint2 unitReactions)
        {
            base.OnReaction(solution, reaction,  randomReagent, owner, unitReactions);

            var coordinates = Transform(owner).Coordinates;

            _adminLogger.Add(LogType.ChemicalReaction, reaction.Impact,
                $"Chemical reaction {reaction.ID:reaction} occurred with strength {unitReactions:strength} on entity {ToPrettyString(owner):metabolizer} at {coordinates}");

            _audioSystem.Play(reaction.Sound, Filter.Pvs(owner, entityManager:EntityManager), owner);
        }
    }
}
