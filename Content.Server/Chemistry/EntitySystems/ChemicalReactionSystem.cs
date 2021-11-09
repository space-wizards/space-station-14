using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems
{
    public class ChemicalReactionSystem : SharedChemicalReactionSystem
    {
        protected override void OnReaction(Solution solution, ReactionPrototype reaction, EntityUid ownerUid, FixedPoint2 unitReactions)
        {
            base.OnReaction(solution, reaction, ownerUid, unitReactions);

            SoundSystem.Play(Filter.Pvs(ownerUid, entityManager:EntityManager), reaction.Sound.GetSound(), ownerUid);
        }
    }
}
