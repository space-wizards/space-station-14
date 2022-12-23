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
        protected override void OnReactionStart(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, out ReactionData data)
        {
            base.OnReactionStart(reaction, uid, solution, curTime, out data);

            var coordinates = Transform(uid).Coordinates;

            _adminLogger.Add(LogType.ChemicalReaction, reaction.Impact,
                $"Chemical reaction {reaction.ID:reaction} started on entity {ToPrettyString(uid):metabolizer} at {coordinates}");

            SoundSystem.Play(reaction.Sound.GetSound(), Filter.Pvs(uid, entityManager:EntityManager), uid);
        }

        protected override void OnReactionStop(ReactionSpecification reaction, EntityUid uid, Solution solution, TimeSpan curTime, ReactionData data)
        {
            base.OnReactionStop(reaction, uid, solution, curTime, data);

            var coordinates = Transform(uid).Coordinates;

            _adminLogger.Add(LogType.ChemicalReaction, reaction.Impact,
                $"Chemical reaction {reaction.ID:reaction} stopped with strength {data:TotalQuantity} on entity {ToPrettyString(uid):metabolizer} at {coordinates}");

            SoundSystem.Play(reaction.Sound.GetSound(), Filter.Pvs(uid, entityManager:EntityManager), uid);
        }
    }
}
