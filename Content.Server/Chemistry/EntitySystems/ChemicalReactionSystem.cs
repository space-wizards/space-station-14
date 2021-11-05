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
        protected override void OnReaction(Solution solution, ReactionPrototype reaction, IEntity owner, FixedPoint2 unitReactions)
        {
            base.OnReaction(solution, reaction, owner, unitReactions);

            SoundSystem.Play(Filter.Pvs(owner), reaction.Sound.GetSound(), owner);
        }
    }
}
