using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems
{
    public class ChemicalReactionSystem : SharedChemicalReactionSystem
    {
        protected override void OnReaction(ReactionPrototype reaction, IEntity owner, ReagentUnit unitReactions)
        {
            base.OnReaction(reaction, owner, unitReactions);

            SoundSystem.Play(Filter.Pvs(owner), reaction.Sound.GetSound(), owner.Transform.Coordinates);
        }
    }
}
