#nullable enable
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

            if (reaction.Sound.TryGetSound(out var sound))
                SoundSystem.Play(Filter.Pvs(owner), sound, owner.Transform.Coordinates);
        }
    }
}
